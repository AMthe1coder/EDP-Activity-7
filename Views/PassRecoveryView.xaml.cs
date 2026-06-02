using MySql.Data.MySqlClient;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SystemUI.Views
{
    public partial class PassRecoveryView : UserControl
    {
        public PassRecoveryView()
        {
            InitializeComponent();
        }
        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string email = txtEmail.Text.Trim();
            string newPassword = txtNewPassword.Password;
            string confirmPass = txtConfirmPassword.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) ||
                string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPass))
            {
                ShowStatus("Please fill in all fields.", false);
                return;
            }

            if (newPassword != confirmPass)
            {
                ShowStatus("Passwords do not match.", false);
                return;
            }

            try
            {
                using (var conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string check = "SELECT COUNT(*) FROM users WHERE username=@u AND email=@e";
                    var cmdCheck = new MySqlCommand(check, conn);
                    cmdCheck.Parameters.AddWithValue("@u", username);
                    cmdCheck.Parameters.AddWithValue("@e", email);

                    int count = Convert.ToInt32(cmdCheck.ExecuteScalar());
                    if (count == 0)
                    {
                        ShowStatus("Username or email not found.", false);
                        return;
                    }

                    string update = "UPDATE users SET password=@p WHERE username=@u AND email=@e";
                    var cmdUpdate = new MySqlCommand(update, conn);
                    cmdUpdate.Parameters.AddWithValue("@p", newPassword);
                    cmdUpdate.Parameters.AddWithValue("@u", username);
                    cmdUpdate.Parameters.AddWithValue("@e", email);
                    cmdUpdate.ExecuteNonQuery();

                    ShowStatus("Password reset successfully!", true);
                    txtUsername.Text = txtEmail.Text = "";
                    txtNewPassword.Password = txtConfirmPassword.Password = "";
                }
            }
            catch
            {
                ShowStatus("Database connection failed.", false);
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