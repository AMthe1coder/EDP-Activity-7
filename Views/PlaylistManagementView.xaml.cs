using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace SystemUI.Views
{
    public partial class PlaylistManagementView : UserControl
    {
        private int selectedId = -1;
        private DispatcherTimer statusTimer;
        private readonly Dictionary<int, PlaylistSong> pendingSongsById = new Dictionary<int, PlaylistSong>();

        public PlaylistManagementView()
        {
            InitializeComponent();
            LoadSongDropdown();
            LoadPlaylists("");
            UpdatePlaylistSongControls();
        }

        private sealed class SongOption
        {
            public int Id { get; }
            public string Display { get; }
            public SongOption(int id, string display) { Id = id; Display = display; }
            public override string ToString() => Display;
        }

        private sealed class PlaylistSong
        {
            public int SongId { get; }
            public string Display { get; }
            public PlaylistSong(int songId, string display) { SongId = songId; Display = display; }
            public override string ToString() => Display;
        }

        private void UpdatePlaylistSongControls()
        {

            cmbSongToAdd.IsEnabled = true;
            btnAddSong.IsEnabled = true;
            btnRemoveSong.IsEnabled = true;
            lstPlaylistSongs.IsEnabled = true;
        }

        private void RefreshSongsView()
        {
            if (selectedId == -1)
            {
                lstPlaylistSongs.ItemsSource = new List<PlaylistSong>(pendingSongsById.Values);
                return;
            }

            LoadPlaylistSongs(selectedId);
        }

        private void LoadSongDropdown()
        {
            try
            {
                using (var conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand("SELECT id, title, artist FROM songs ORDER BY title", conn);
                    using (var reader = cmd.ExecuteReader())
                    {
                        var items = new List<SongOption>();
                        while (reader.Read())
                        {
                            int id = Convert.ToInt32(reader["id"]);
                            string title = reader["title"].ToString();
                            string artist = reader["artist"].ToString();
                            items.Add(new SongOption(id, $"{title} — {artist}"));
                        }
                        cmbSongToAdd.ItemsSource = items;
                        cmbSongToAdd.DisplayMemberPath = "Display";
                        if (items.Count == 0)
                            ShowStatus("No songs in library yet. Add songs in Songs first.", false);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load songs: " + ex.Message, "Error");
            }
        }

        private void LoadPlaylistSongs(int playlistId)
        {
            try
            {
                using (var conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand(@"
SELECT s.id, s.title, s.artist
FROM playlist_songs ps
JOIN songs s ON s.id = ps.song_id
WHERE ps.playlist_id = @pid
ORDER BY s.title, s.artist", conn);
                    cmd.Parameters.AddWithValue("@pid", playlistId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        var items = new List<PlaylistSong>();
                        while (reader.Read())
                        {
                            int sid = Convert.ToInt32(reader["id"]);
                            string title = reader["title"].ToString();
                            string artist = reader["artist"].ToString();
                            items.Add(new PlaylistSong(sid, $"{title} — {artist}"));
                        }
                        lstPlaylistSongs.ItemsSource = items;
                    }
                }
            }
            catch (Exception ex)
            {
                ShowStatus("Error loading playlist songs: " + ex.Message, false);
            }
        }

        private void LoadPlaylists(string search)
        {
            try
            {
                using (var conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = string.IsNullOrEmpty(search)
                        ? "SELECT id, name, owner, description, created_at FROM playlists ORDER BY id"
                        : "SELECT id, name, owner, description, created_at FROM playlists WHERE name LIKE @s OR owner LIKE @s ORDER BY id";
                    var cmd = new MySqlCommand(query, conn);
                    if (!string.IsNullOrEmpty(search)) cmd.Parameters.AddWithValue("@s", $"%{search}%");
                    var adapter = new MySqlDataAdapter(cmd);
                    var table = new DataTable();
                    adapter.Fill(table);
                    dgPlaylists.ItemsSource = table.DefaultView;
                }
            }
            catch { MessageBox.Show("Failed to load playlists.", "Error"); }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string name = txtName.Text.Trim();
            string owner = txtOwner.Text.Trim();
            string desc = txtDescription.Text.Trim();

            if (string.IsNullOrEmpty(name)) { ShowStatus("Playlist name is required.", false); return; }

            try
            {
                using (var conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    if (selectedId == -1)
                    {
                        var cmd = new MySqlCommand("INSERT INTO playlists (name, owner, description) VALUES (@n,@o,@d)", conn);
                        cmd.Parameters.AddWithValue("@n", name);
                        cmd.Parameters.AddWithValue("@o", owner);
                        cmd.Parameters.AddWithValue("@d", desc);
                        cmd.ExecuteNonQuery();
                        selectedId = (int)cmd.LastInsertedId;
                        lblFormTitle.Text = "Update Playlist";
                        btnSave.Content = "Update";
                        UpdatePlaylistSongControls();
                        // Persist any songs added before Save.
                        if (pendingSongsById.Count > 0)
                        {
                            using (var tx = conn.BeginTransaction())
                            {
                                foreach (var kv in pendingSongsById)
                                {
                                    var insertCmd = new MySqlCommand(
                                        "INSERT IGNORE INTO playlist_songs (playlist_id, song_id) VALUES (@pid, @sid)",
                                        conn, tx);
                                    insertCmd.Parameters.AddWithValue("@pid", selectedId);
                                    insertCmd.Parameters.AddWithValue("@sid", kv.Key);
                                    insertCmd.ExecuteNonQuery();
                                }

                                tx.Commit();
                            }
                        }

                        pendingSongsById.Clear();
                        RefreshSongsView();
                        ShowStatus("Playlist saved.", true);
                    }
                    else
                    {
                        var cmd = new MySqlCommand("UPDATE playlists SET name=@n, owner=@o, description=@d WHERE id=@id", conn);
                        cmd.Parameters.AddWithValue("@n", name);
                        cmd.Parameters.AddWithValue("@o", owner);
                        cmd.Parameters.AddWithValue("@d", desc);
                        cmd.Parameters.AddWithValue("@id", selectedId);
                        cmd.ExecuteNonQuery();
                        ShowStatus("Playlist updated!", true);
                    }
                    LoadPlaylists("");
                }
            }
            catch (Exception ex) { ShowStatus("Error: " + ex.Message, false); }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (selectedId == -1) { ShowStatus("Select a playlist to delete.", false); return; }
            if (MessageBox.Show("Delete this playlist?", "Confirm", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            try
            {
                using (var conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand("DELETE FROM playlists WHERE id=@id", conn);
                    cmd.Parameters.AddWithValue("@id", selectedId);
                    cmd.ExecuteNonQuery();
                    ShowStatus("Playlist deleted.", true);
                    BtnClear_Click(null, null);
                    LoadPlaylists("");
                }
            }
            catch (Exception ex) { ShowStatus("Error: " + ex.Message, false); }
        }

        private void BtnAddSong_Click(object sender, RoutedEventArgs e)
        {
            if (!(cmbSongToAdd.SelectedItem is SongOption song))
            {
                ShowStatus("Select a song to add.", false);
                return;
            }

            if (selectedId == -1)
            {
                if (!pendingSongsById.ContainsKey(song.Id))
                    pendingSongsById[song.Id] = new PlaylistSong(song.Id, song.Display);

                RefreshSongsView();
                ShowStatus("Song added (press Save to create playlist).", true);
                return;
            }

            try
            {
                using (var conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand("INSERT IGNORE INTO playlist_songs (playlist_id, song_id) VALUES (@pid, @sid)", conn);
                    cmd.Parameters.AddWithValue("@pid", selectedId);
                    cmd.Parameters.AddWithValue("@sid", song.Id);
                    cmd.ExecuteNonQuery();
                }

                RefreshSongsView();
                ShowStatus("Song added to playlist.", true);
            }
            catch (Exception ex)
            {
                ShowStatus("Error: " + ex.Message, false);
            }
        }

        private void BtnRemoveSong_Click(object sender, RoutedEventArgs e)
        {
            if (!(lstPlaylistSongs.SelectedItem is PlaylistSong song))
            {
                ShowStatus("Select a song in the playlist to remove.", false);
                return;
            }

            if (selectedId == -1)
            {
                if (pendingSongsById.Remove(song.SongId))
                    RefreshSongsView();

                ShowStatus("Pending song removed.", true);
                return;
            }

            try
            {
                using (var conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand("DELETE FROM playlist_songs WHERE playlist_id=@pid AND song_id=@sid", conn);
                    cmd.Parameters.AddWithValue("@pid", selectedId);
                    cmd.Parameters.AddWithValue("@sid", song.SongId);
                    cmd.ExecuteNonQuery();
                }

                RefreshSongsView();
                ShowStatus("Song removed from playlist.", true);
            }
            catch (Exception ex)
            {
                ShowStatus("Error: " + ex.Message, false);
            }
        }

        private void DgPlaylists_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgPlaylists.SelectedItem is DataRowView row)
            {
                selectedId = Convert.ToInt32(row["id"]);
                pendingSongsById.Clear();
                txtName.Text = row["name"].ToString();
                txtOwner.Text = row["owner"].ToString();
                txtDescription.Text = row["description"].ToString();
                lblFormTitle.Text = "Update Playlist";
                btnSave.Content = "Update";

                UpdatePlaylistSongControls();
                RefreshSongsView();
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            selectedId = -1;
            pendingSongsById.Clear();
            txtName.Text = txtOwner.Text = txtDescription.Text = "";
            lblFormTitle.Text = "Add Playlist";
            btnSave.Content = "Save";
            lblFormStatus.Visibility = Visibility.Collapsed;
            lstPlaylistSongs.ItemsSource = new List<PlaylistSong>();
            cmbSongToAdd.SelectedIndex = -1;
            UpdatePlaylistSongControls();
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e) => LoadPlaylists(txtSearch.Text.Trim());
        private void BtnLoadAll_Click(object sender, RoutedEventArgs e) { txtSearch.Text = ""; LoadPlaylists(""); }

        private void Nav_Dashboard(object sender, MouseButtonEventArgs e)
            => ((MainWindow)Window.GetWindow(this)).MainContent.Content = new DashboardView();
        private void Nav_Songs(object sender, MouseButtonEventArgs e)
            => ((MainWindow)Window.GetWindow(this)).MainContent.Content = new SongManagementView();
        private void Nav_Playlists(object sender, MouseButtonEventArgs e)
            => ((MainWindow)Window.GetWindow(this)).MainContent.Content = new PlaylistManagementView();
        private void Nav_Borrowing(object sender, MouseButtonEventArgs e)
            => ((MainWindow)Window.GetWindow(this)).MainContent.Content = new BorrowingView();
        private void Nav_Reports(object sender, MouseButtonEventArgs e)
            => ((MainWindow)Window.GetWindow(this)).MainContent.Content = new ReportGeneratorView();
        private void Nav_Users(object sender, MouseButtonEventArgs e)
            => ((MainWindow)Window.GetWindow(this)).MainContent.Content = new UserManagementView();
        private void Nav_Logout(object sender, MouseButtonEventArgs e)
        {
            var main = (MainWindow)Window.GetWindow(this);
            main.ShowMainNav(true);
            main.MainContent.Content = null;
        }

        private void ShowStatus(string msg, bool ok)
        {
            lblFormStatus.Text = msg;
            lblFormStatus.Foreground = ok ? System.Windows.Media.Brushes.LightGreen : System.Windows.Media.Brushes.OrangeRed;
            lblFormStatus.Visibility = Visibility.Visible;

            // Auto-hide success messages so the "green thing" doesn't stick around.
            if (statusTimer != null) statusTimer.Stop();
            if (ok)
            {
                statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.5) };
                statusTimer.Tick += (s, e) =>
                {
                    if (statusTimer != null) statusTimer.Stop();
                    lblFormStatus.Visibility = Visibility.Collapsed;
                };
                statusTimer.Start();
            }
        }
    }
}