using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SystemUI.Views
{
    public partial class AboutView : UserControl
    {
        public AboutView()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                var main = Window.GetWindow(this) as MainWindow;
                main?.ShowMainNav(false);
            };
        }

        private void Nav_Login(object sender, MouseButtonEventArgs e)
        {
            var main = Window.GetWindow(this) as MainWindow;
            if (main == null) return;
            main.ShowMainNav(false);
            main.MainContent.Content = new LoginView();
        }

        private void Nav_Home(object sender, MouseButtonEventArgs e)
        {
            var main = Window.GetWindow(this) as MainWindow;
            if (main == null) return;
            main.ShowMainNav(true);
            main.MainContent.Content = null;
        }
    }
}