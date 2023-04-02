using LaucnherYouTube.ChildWindow;
using System.Windows;

namespace LaucnherYouTube
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void StartUpArgumentsInitialMethod(object sender, StartupEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            if(e.Args.Length >= 1)
            {
                mainWindow.SendersArgumentsTextBlock.Text += string.Join(" ; ", e.Args);
            }
            mainWindow.Show();
        }
    }
}
