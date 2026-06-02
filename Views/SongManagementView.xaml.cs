using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SystemUI.Views
{
    public partial class SongManagementView : UserControl
    {
        private int selectedId = -1;

        public SongManagementView()
        {
            InitializeComponent();
            LoadSongs("");
        }

        private void LoadSongs(string search)
        {
            try
            {
                using (var conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = string.IsNullOrEmpty(search)
                        ? "SELECT id, title, artist, album, genre, duration, year_released FROM songs ORDER BY id"
                        : "SELECT id, title, artist, album, genre, duration, year_released FROM songs WHERE title LIKE @s OR artist LIKE @s OR album LIKE @s OR genre LIKE @s ORDER BY id";
                    var cmd = new MySqlCommand(query, conn);
                    if (!string.IsNullOrEmpty(search))
                        cmd.Parameters.AddWithValue("@s", $"%{search}%");
                    var adapter = new MySqlDataAdapter(cmd);
                    var table = new DataTable();
                    adapter.Fill(table);
                    dgSongs.ItemsSource = table.DefaultView;
                }
            }
            catch { MessageBox.Show("Failed to load songs.", "Error"); }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string title = txtTitle.Text.Trim();
            string artist = txtArtist.Text.Trim();
            string album = txtAlbum.Text.Trim();
            string genre = txtGenre.Text.Trim();
            string duration = txtDuration.Text.Trim();
            string year = txtYear.Text.Trim();

            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(artist))
            {
                ShowStatus("Title and Artist are required.", false);
                return;
            }

            try
            {
                using (var conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    if (selectedId == -1)
                    {
                        var cmd = new MySqlCommand("INSERT INTO songs (title, artist, album, genre, duration, year_released) VALUES (@t,@a,@al,@g,@d,@y)", conn);
                        cmd.Parameters.AddWithValue("@t", title);
                        cmd.Parameters.AddWithValue("@a", artist);
                        cmd.Parameters.AddWithValue("@al", album);
                        cmd.Parameters.AddWithValue("@g", genre);
                        cmd.Parameters.AddWithValue("@d", duration);
                        cmd.Parameters.AddWithValue("@y", string.IsNullOrEmpty(year) ? (object)DBNull.Value : int.Parse(year));
                        cmd.ExecuteNonQuery();
                        ShowStatus("Song added successfully!", true);
                    }
                    else
                    {
                        var cmd = new MySqlCommand("UPDATE songs SET title=@t, artist=@a, album=@al, genre=@g, duration=@d, year_released=@y WHERE id=@id", conn);
                        cmd.Parameters.AddWithValue("@t", title);
                        cmd.Parameters.AddWithValue("@a", artist);
                        cmd.Parameters.AddWithValue("@al", album);
                        cmd.Parameters.AddWithValue("@g", genre);
                        cmd.Parameters.AddWithValue("@d", duration);
                        cmd.Parameters.AddWithValue("@y", string.IsNullOrEmpty(year) ? (object)DBNull.Value : int.Parse(year));
                        cmd.Parameters.AddWithValue("@id", selectedId);
                        cmd.ExecuteNonQuery();
                        ShowStatus("Song updated successfully!", true);
                    }
                    BtnClear_Click(null, null);
                    LoadSongs("");
                }
            }
            catch (Exception ex) { ShowStatus("Error: " + ex.Message, false); }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (selectedId == -1) { ShowStatus("Select a song to delete.", false); return; }
            if (MessageBox.Show("Delete this song?", "Confirm", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            try
            {
                using (var conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand("DELETE FROM songs WHERE id=@id", conn);
                    cmd.Parameters.AddWithValue("@id", selectedId);
                    cmd.ExecuteNonQuery();
                    ShowStatus("Song deleted.", true);
                    BtnClear_Click(null, null);
                    LoadSongs("");
                }
            }
            catch (Exception ex) { ShowStatus("Error: " + ex.Message, false); }
        }

        private void DgSongs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgSongs.SelectedItem is DataRowView row)
            {
                selectedId = Convert.ToInt32(row["id"]);
                txtTitle.Text = row["title"].ToString();
                txtArtist.Text = row["artist"].ToString();
                txtAlbum.Text = row["album"].ToString();
                txtGenre.Text = row["genre"].ToString();
                txtDuration.Text = row["duration"].ToString();
                txtYear.Text = row["year_released"].ToString();
                lblFormTitle.Text = "Update Song";
                btnSave.Content = "Update";
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            selectedId = -1;
            txtTitle.Text = txtArtist.Text = txtAlbum.Text = txtGenre.Text = txtDuration.Text = txtYear.Text = "";
            lblFormTitle.Text = "Add Song";
            btnSave.Content = "Save";
            lblFormStatus.Visibility = Visibility.Collapsed;
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e) => LoadSongs(txtSearch.Text.Trim());
        private void BtnLoadAll_Click(object sender, RoutedEventArgs e) { txtSearch.Text = ""; LoadSongs(""); }

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
        }
    }
}