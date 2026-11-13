using System.Windows;

namespace FortniteChestCounter;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        Loc.Init(AppSettings.Load().Language);
        var app = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
        app.Run(new MainWindow());
    }
}
