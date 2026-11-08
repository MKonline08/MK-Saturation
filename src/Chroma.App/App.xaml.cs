using System;
using System.IO;
using System.Threading;
using System.Windows;
namespace MkSaturation;
public partial class App : System.Windows.Application
{
    private Mutex? single;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        single = new Mutex(true, @"Local\MK.Saturation.Session", out bool created);
        if (!created) { MessageBox.Show("MK Saturation is already running. Open it from the notification area.", "MK Saturation"); Shutdown(); return; }
        if (Array.IndexOf(e.Args, "--diagnose") >= 0 || Array.IndexOf(e.Args, "--native-smoke") >= 0)
        {
            int result = 0;
            string report = Diagnostics.Run(Array.IndexOf(e.Args, "--native-smoke") >= 0, out result);
            var target = e.Args.Length > 1 ? e.Args[1] : Path.Combine(Path.GetTempPath(), "MK-Saturation-diagnostics.txt");
            try { File.WriteAllText(target, report); } catch { result = 2; }
            Shutdown(result); return;
        }
        DispatcherUnhandledException += (_, args) =>
        {
            if (MainWindow is MainWindow window) window.EmergencyRestore();
            MessageBox.Show(args.Exception.Message, "MK Saturation — unexpected error");
            args.Handled = true; Shutdown(1);
        };
        MainWindow = new MainWindow(); MainWindow.Show();
    }
    protected override void OnExit(ExitEventArgs e) { single?.Dispose(); base.OnExit(e); }
}
