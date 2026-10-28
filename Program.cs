using System;
using System.Threading;
using System.Windows;

namespace WinSpace;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Only one instance at a time.
        using var mutex = new Mutex(true, "WinSpace.SingleInstance", out bool isNew);
        if (!isNew) return;

        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        var manager = new DesktopLabelManager();
        manager.Start();
        app.Run();
        manager.Dispose();
    }
}
