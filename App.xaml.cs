using System.Windows;

namespace PixelDogReminders;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        bool isStartup = false;
        if (e.Args != null && e.Args.Length > 0)
        {
            isStartup = e.Args.Any(a => a.Equals("--startup", StringComparison.OrdinalIgnoreCase));
        }

        var mainWindow = new MainWindow(isStartup);
        if (!isStartup)
        {
            mainWindow.Show();
        }
    }
}
