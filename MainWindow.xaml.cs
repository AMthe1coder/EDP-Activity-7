using System.Windows;
using System.Windows.Input;
using SystemUI.Views;

namespace SystemUI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public void ShowMainNav(bool visible)
        {
            TopNav.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Nav_Login(object sender, MouseButtonEventArgs e)
        {
            ShowMainNav(false);
            MainContent.Content = new LoginView();
        }

        private void Nav_About(object sender, MouseButtonEventArgs e)
        {
            ShowMainNav(false);
            MainContent.Content = new AboutView();
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            ShowMainNav(false);
            MainContent.Content = new LoginView();
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            ShowMainNav(false);
            MainContent.Content = new RegisterView();
        }
    }
}