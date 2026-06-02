using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SystemUI.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
            Loaded += (s, e) => LoadDashboard();
        }

        private void LoadDashboard()
        {
            try
            {
                using (var conn = DBConnection.GetConnection())
                {
                    conn.Open();

                    // Song count
                    var cmd = new MySqlCommand("SELECT COUNT(*) FROM songs", conn);
                    lblSongCount.Text = cmd.ExecuteScalar().ToString();

                    // Playlist count
                    cmd = new MySqlCommand("SELECT COUNT(*) FROM playlists", conn);
                    lblPlaylistCount.Text = cmd.ExecuteScalar().ToString();

                    // Active borrows
                    cmd = new MySqlCommand("SELECT COUNT(*) FROM borrowing WHERE status='borrowed'", conn);
                    lblBorrowCount.Text = cmd.ExecuteScalar().ToString();

                    // Recent songs
                    cmd = new MySqlCommand("SELECT title, artist, genre FROM songs ORDER BY id DESC LIMIT 10", conn);
                    var adapter = new MySqlDataAdapter(cmd);
                    var table = new DataTable();
                    adapter.Fill(table);
                    dgRecentSongs.ItemsSource = table.DefaultView;
                }
            }
            catch
            {
                lblSongCount.Text = "—";
                lblPlaylistCount.Text = "—";
                lblBorrowCount.Text = "—";
            }
        }

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
    }
}