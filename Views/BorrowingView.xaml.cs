using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SystemUI.Views
{
    public partial class BorrowingView : UserControl
    {
        private int selectedId = -1;

        public BorrowingView()
        {
            InitializeComponent();
            LoadDropdowns();
            LoadBorrowing("");
            txtBorrowDate.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        }

        private void LoadDropdowns()
        {
            try
            {
                using (var conn = DBConnection.GetConnection())
                {
                    conn.Open();

                    // Load Users full_name
                    cmbBorrower.Items.Clear();
                    var cmdUsers = new MySqlCommand("SELECT full_name FROM users ORDER BY full_name", conn);
                    using (var reader = cmdUsers.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            cmbBorrower.Items.Add(reader.GetString("full_name"));
                        }
                    }

                    // Load Songs title
                    cmbSongTitle.Items.Clear();
                    var cmdSongs = new MySqlCommand("SELECT title FROM songs ORDER BY title", conn);
                    using (var reader = cmdSongs.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            cmbSongTitle.Items.Add(reader.GetString("title"));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load drop downs: " + ex.Message, "Error");
            }
        }

        private void LoadBorrowing(string search)
        {
            try
            {
                using (var conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = string.IsNullOrEmpty(search)
                        ? "SELECT id, borrower_name, song_title, borrow_date, return_date, status, remarks FROM borrowing ORDER BY id DESC"
                        : "SELECT id, borrower_name, song_title, borrow_date, return_date, status, remarks FROM borrowing WHERE borrower_name LIKE @s OR song_title LIKE @s OR status LIKE @s ORDER BY id DESC";
                    var cmd = new MySqlCommand(query, conn);
                    if (!string.IsNullOrEmpty(search)) cmd.Parameters.AddWithValue("@s", $"%{search}%");
                    var adapter = new MySqlDataAdapter(cmd);
                    var table = new DataTable();
                    adapter.Fill(table);
                    dgBorrowing.ItemsSource = table.DefaultView;
                }
            }
            catch { MessageBox.Show("Failed to load borrowing records.", "Error"); }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string borrower = cmbBorrower.SelectedItem?.ToString() ?? cmbBorrower.Text.Trim();
            string song = cmbSongTitle.SelectedItem?.ToString() ?? cmbSongTitle.Text.Trim();
            // Always use actual current time for NEW borrows (no manual typing).
            string borrowDate = selectedId == -1
                ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                : txtBorrowDate.Text.Trim();
            string returnDate = dpReturnDate.SelectedDate.HasValue 
                ? dpReturnDate.SelectedDate.Value.ToString("yyyy-MM-dd") 
                : null;
            string status = (cmbBorrowStatus.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "borrowed";
            string remarks = txtRemarks.Text.Trim();

            if (string.IsNullOrEmpty(borrower) || string.IsNullOrEmpty(song))
            {
                ShowStatus("Borrower and Song are required.", false);
                return;
            }

            try
            {
                using (var conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    if (selectedId == -1)
                    {
                        var cmd = new MySqlCommand("INSERT INTO borrowing (borrower_name, song_title, borrow_date, return_date, status, remarks) VALUES (@b,@s,@bd,@rd,@st,@r)", conn);
                        cmd.Parameters.AddWithValue("@b", borrower);
                        cmd.Parameters.AddWithValue("@s", song);
                        cmd.Parameters.AddWithValue("@bd", borrowDate);
                        cmd.Parameters.AddWithValue("@rd", string.IsNullOrEmpty(returnDate) ? (object)DBNull.Value : returnDate);
                        cmd.Parameters.AddWithValue("@st", status);
                        cmd.Parameters.AddWithValue("@r", remarks);
                        cmd.ExecuteNonQuery();
                        ShowStatus("Borrow record added!", true);
                    }
                    else
                    {
                        var cmd = new MySqlCommand("UPDATE borrowing SET borrower_name=@b, song_title=@s, borrow_date=@bd, return_date=@rd, status=@st, remarks=@r WHERE id=@id", conn);
                        cmd.Parameters.AddWithValue("@b", borrower);
                        cmd.Parameters.AddWithValue("@s", song);
                        cmd.Parameters.AddWithValue("@bd", borrowDate);
                        cmd.Parameters.AddWithValue("@rd", string.IsNullOrEmpty(returnDate) ? (object)DBNull.Value : returnDate);
                        cmd.Parameters.AddWithValue("@st", status);
                        cmd.Parameters.AddWithValue("@r", remarks);
                        cmd.Parameters.AddWithValue("@id", selectedId);
                        cmd.ExecuteNonQuery();
                        ShowStatus("Record updated!", true);
                    }
                    BtnClear_Click(null, null);
                    LoadBorrowing("");
                }
            }
            catch (Exception ex) { ShowStatus("Error: " + ex.Message, false); }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (selectedId == -1) { ShowStatus("Select a record to delete.", false); return; }
            if (MessageBox.Show("Delete this record?", "Confirm", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            try
            {
                using (var conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand("DELETE FROM borrowing WHERE id=@id", conn);
                    cmd.Parameters.AddWithValue("@id", selectedId);
                    cmd.ExecuteNonQuery();
                    ShowStatus("Record deleted.", true);
                    BtnClear_Click(null, null);
                    LoadBorrowing("");
                }
            }
            catch (Exception ex) { ShowStatus("Error: " + ex.Message, false); }
        }

        private void DgBorrowing_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgBorrowing.SelectedItem is DataRowView row)
            {
                selectedId = Convert.ToInt32(row["id"]);
                cmbBorrower.SelectedItem = row["borrower_name"].ToString();
                if (cmbBorrower.SelectedItem == null) cmbBorrower.Text = row["borrower_name"].ToString();

                cmbSongTitle.SelectedItem = row["song_title"].ToString();
                if (cmbSongTitle.SelectedItem == null) cmbSongTitle.Text = row["song_title"].ToString();

                txtBorrowDate.Text = Convert.ToDateTime(row["borrow_date"]).ToString("yyyy-MM-dd HH:mm");

                if (row["return_date"] != DBNull.Value && !string.IsNullOrEmpty(row["return_date"].ToString()))
                {
                    dpReturnDate.SelectedDate = Convert.ToDateTime(row["return_date"]);
                }
                else
                {
                    dpReturnDate.SelectedDate = null;
                }

                txtRemarks.Text = row["remarks"].ToString();
                string st = row["status"].ToString();
                foreach (ComboBoxItem item in cmbBorrowStatus.Items)
                    if (item.Content.ToString() == st) cmbBorrowStatus.SelectedItem = item;
                lblFormTitle.Text = "Update Record";
                btnSave.Content = "Update";
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            selectedId = -1;
            cmbBorrower.SelectedIndex = -1;
            cmbBorrower.Text = "";
            cmbSongTitle.SelectedIndex = -1;
            cmbSongTitle.Text = "";
            txtBorrowDate.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            dpReturnDate.SelectedDate = null;
            txtRemarks.Text = "";
            cmbBorrowStatus.SelectedIndex = 0;
            lblFormTitle.Text = "New Borrow Record";
            btnSave.Content = "Save";
            lblFormStatus.Visibility = Visibility.Collapsed;
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e) => LoadBorrowing(txtSearch.Text.Trim());
        private void BtnLoadAll_Click(object sender, RoutedEventArgs e) { txtSearch.Text = ""; LoadBorrowing(""); }

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