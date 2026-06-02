using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SystemUI.Views
{
    public partial class UserManagementView : UserControl
    {
        private int selectedId = -1;

        public UserManagementView()
        {
            InitializeComponent();
            LoadUsers("");
        }

        private void LoadUsers(string search)
        {
            try
            {
                using (var conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = string.IsNullOrEmpty(search)
                        ? "SELECT id, full_name, username, email, status, created_at FROM users ORDER BY id"
                        : "SELECT id, full_name, username, email, status, created_at FROM users WHERE username LIKE @s OR full_name LIKE @s OR email LIKE @s ORDER BY id";

                    var cmd = new MySqlCommand(query, conn);
                    if (!string.IsNullOrEmpty(search))
                        cmd.Parameters.AddWithValue("@s", $"%{search}%");

                    var adapter = new MySqlDataAdapter(cmd);
                    var table = new DataTable();
                    adapter.Fill(table);
                    dgUsers.ItemsSource = table.DefaultView;
                }
            }
            catch
            {
                MessageBox.Show("Failed to load users.", "Error");
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string fullName = txtFullName.Text.Trim();
            string username = txtUser.Text.Trim();
            string email = txtUserEmail.Text.Trim();
            string password = txtUserPass.Password;
            string status = (cmbStatus.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "active";

            if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(username) ||
                string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ShowFormStatus("Please fill in all fields.", false);
                return;
            }

            try
            {
                using (var conn = DBConnection.GetConnection())
                {
                    conn.Open();

                    if (selectedId == -1)
                    {
                        string insert = "INSERT INTO users (full_name, username, email, password, status) VALUES (@fn, @u, @e, @p, @s)";
                        var cmd = new MySqlCommand(insert, conn);
                        cmd.Parameters.AddWithValue("@fn", fullName);
                        cmd.Parameters.AddWithValue("@u", username);
                        cmd.Parameters.AddWithValue("@e", email);
                        cmd.Parameters.AddWithValue("@p", password);
                        cmd.Parameters.AddWithValue("@s", status);
                        cmd.ExecuteNonQuery();
                        ShowFormStatus("Account added successfully!", true);
                    }
                    else
                    {
                        string update = "UPDATE users SET full_name=@fn, username=@u, email=@e, password=@p, status=@s WHERE id=@id";
                        var cmd = new MySqlCommand(update, conn);
                        cmd.Parameters.AddWithValue("@fn", fullName);
                        cmd.Parameters.AddWithValue("@u", username);
                        cmd.Parameters.AddWithValue("@e", email);
                        cmd.Parameters.AddWithValue("@p", password);
                        cmd.Parameters.AddWithValue("@s", status);
                        cmd.Parameters.AddWithValue("@id", selectedId);
                        cmd.ExecuteNonQuery();
                        ShowFormStatus("Account updated successfully!", true);
                    }

                    BtnClear_Click(null, null);
                    LoadUsers("");
                }
            }
            catch (Exception ex)
            {
                ShowFormStatus(ex.Message.Contains("Duplicate")
                    ? "Username already exists."
                    : "Error: " + ex.Message, false);
            }
        }
        private void DgUsers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgUsers.SelectedItem is DataRowView row)
            {
                selectedId = Convert.ToInt32(row["id"]);
                txtFullName.Text = row["full_name"].ToString();
                txtUser.Text = row["username"].ToString();
                txtUserEmail.Text = row["email"].ToString();
                txtUserPass.Password = "";

                string status = row["status"].ToString();
                foreach (ComboBoxItem item in cmbStatus.Items)
                    if (item.Content.ToString() == status)
                        cmbStatus.SelectedItem = item;

                lblFormTitle.Text = "Update Account";
                btnSave.Content = "Update";
            }
        }
        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            selectedId = -1;
            txtFullName.Text = "";
            txtUser.Text = "";
            txtUserEmail.Text = "";
            txtUserPass.Password = "";
            cmbStatus.SelectedIndex = 0;
            lblFormTitle.Text = "Add Account";
            btnSave.Content = "Save";
            lblFormStatus.Visibility = Visibility.Collapsed;
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
            => LoadUsers(txtSearch.Text.Trim());

        private void BtnLoadAll_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            LoadUsers("");
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
        private void ShowFormStatus(string message, bool success)
        {
            lblFormStatus.Text = message;
            lblFormStatus.Foreground = success
                ? System.Windows.Media.Brushes.LightGreen
                : System.Windows.Media.Brushes.OrangeRed;
            lblFormStatus.Visibility = Visibility.Visible;
        }
    }
}