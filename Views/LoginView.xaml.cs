using MySql.Data.MySqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SystemUI.Views
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
        }
        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                lblError.Text = "Please enter username and password.";
                lblError.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                using (var conn = DBConnection.GetConnection())
                {
                    conn.Open();
                    string query = "SELECT * FROM users WHERE username=@u AND password=@p AND status='active'";
                    var cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@u", username);
                    cmd.Parameters.AddWithValue("@p", password);

                    var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        var main = (MainWindow)Window.GetWindow(this);
                        main.ShowMainNav(false);
                        main.MainContent.Content = new DashboardView();
                    }
                    else
                    {
                        lblError.Text = "Invalid credentials or account is inactive.";
                        lblError.Visibility = Visibility.Visible;
                    }
                }
            }
            catch
            {
                lblError.Text = "Database connection failed.";
                lblError.Visibility = Visibility.Visible;
            }
        }
        private void LnkForgotPassword_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var main = (MainWindow)Window.GetWindow(this);
            main.MainContent.Content = new PassRecoveryView();
        }
        private void LnkRegister_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var main = (MainWindow)Window.GetWindow(this);
            main.MainContent.Content = new RegisterView();
        }
    }
}