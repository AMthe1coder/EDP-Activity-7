using MySql.Data.MySqlClient;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SystemUI.Views
{
    public partial class RegisterView : UserControl
    {
        public RegisterView()
        {
            InitializeComponent();
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            string fullName = txtFullName.Text.Trim();
            string username = txtUsername.Text.Trim();
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Password;
            string confirm = txtConfirm.Password;

            if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(username) ||
                string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ShowStatus("Please fill in all fields.", false);
                return;
            }

            if (password != confirm)
            {
                ShowStatus("Passwords do not match.", false);
                return;
            }

            try
            {
                using (var conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string insert = "INSERT INTO users (full_name, username, email, password, status) VALUES (@fn, @u, @e, @p, 'active')";
                    var cmd = new MySqlCommand(insert, conn);
                    cmd.Parameters.AddWithValue("@fn", fullName);
                    cmd.Parameters.AddWithValue("@u", username);
                    cmd.Parameters.AddWithValue("@e", email);
                    cmd.Parameters.AddWithValue("@p", password);
                    cmd.ExecuteNonQuery();

                    ShowStatus("Account created! You can now login.", true);
                }
            }
            catch (Exception ex)
            {
                ShowStatus(ex.Message.Contains("Duplicate")
                    ? "Username already exists."
                    : "Database error: " + ex.Message, false);
            }
        }
        private void LnkLogin_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var main = (MainWindow)Window.GetWindow(this);
            main.MainContent.Content = new LoginView();
        }
        private void ShowStatus(string msg, bool success)
        {
            lblStatus.Text = msg;
            lblStatus.Foreground = success
                ? System.Windows.Media.Brushes.LightGreen
                : System.Windows.Media.Brushes.OrangeRed;
            lblStatus.Visibility = Visibility.Visible;
        }
    }
}