using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using Forms = System.Windows.Forms;
namespace MkSaturation;
public partial class MainWindow : Window
{
    private readonly SettingsStore store = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MK Saturation", "settings.json"));
    private NativeBackend? backend;
    private EffectController? controller;
    private readonly DispatcherTimer live = new() { Interval = TimeSpan.FromMilliseconds(35) };
    private readonly DispatcherTimer save = new() { Interval = TimeSpan.FromMilliseconds(350) };
    private readonly DispatcherTimer monitor = new() { Interval = TimeSpan.FromSeconds(2) };
    private Forms.NotifyIcon? tray;
    private bool ready, comparing, exiting;
    private string lastError = "None";
    private int Percent => (int)Math.Round(Amount.Value);
    public MainWindow()
    {
        InitializeComponent();
        var settings = store.Load();
        Amount.Value = settings.Saturation; Global.IsChecked = settings.GlobalEnabled; Minimized.IsChecked = settings.StartMinimized;
        try { Startup.IsChecked = StartupRegistration.IsEnabled(); } catch (Exception ex) { Notice.Text = ex.Message; }
        if (store.Warning != null) Notice.Text = store.Warning;
        live.Tick += (_, _) => { live.Stop(); Apply(); };
        save.Tick += (_, _) => { save.Stop(); Save(); };
        monitor.Tick += (_, _) => { try { controller?.CheckOwnership(); } catch (Exception ex) { Fail(ex); } };
        InitializeBackend();
        tray = new Forms.NotifyIcon { Text = "MK Saturation", Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? System.Drawing.SystemIcons.Application, Visible = true };
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Open MK Saturation", null, (_, _) => Dispatcher.Invoke(ShowWindow));
        menu.Items.Add("Reset to 100%", null, (_, _) => Dispatcher.Invoke(() => { comparing = false; Amount.Value = 100; Apply(); QueueSave(); }));
        menu.Items.Add("Restore and exit", null, (_, _) => Dispatcher.Invoke(Close));
        tray.ContextMenuStrip = menu;
        tray.DoubleClick += (_, _) => Dispatcher.Invoke(ShowWindow);
        SystemEvents.SessionSwitch += SessionSwitch;
        SystemEvents.PowerModeChanged += PowerChanged;
        ready = true;
        Apply(); monitor.Start();
        Loaded += (_, _) => { if (settings.StartMinimized && Environment.GetCommandLineArgs().Contains("--startup")) Hide(); };
        Closing += OnClosing;
        System.Windows.Application.Current.SessionEnding += SessionEnding;
    }
    private void InitializeBackend()
    {
        if (controller?.OwnsEffect == true) return;
        backend?.Dispose(); backend = null; controller = null;
        try { backend = new NativeBackend(); _ = backend.Read(); controller = new EffectController(backend); }
        catch (Exception ex)
        {
            lastError = ex.Message; Global.IsChecked = false;
            Status.Text = "Local preview only · " + ex.Message;
            Notice.Text = "Desktop backend unavailable: " + ex.Message + " Local preview is available. Use Retry backend after resolving the problem.";
        }
    }
    private void ShowWindow() { Show(); WindowState = WindowState.Normal; Activate(); }
    private void AmountChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!ready) return;
        ValueLabel.Text = Percent + "%";
        live.Stop(); live.Start(); QueueSave();
    }
    private void Apply()
    {
        if (!ready) return;
        ValueLabel.Text = Percent + "%";
        try
        {
            if (Global.IsChecked == true)
            {
                if (controller == null) throw new InvalidOperationException("Global backend unavailable. Use Retry backend, or keep the local preview.");
                controller.Apply(comparing ? 100 : Percent);
                Status.Text = comparing ? "Comparing · neutral matrix accepted by Windows" : "Desktop matrix accepted · verify the visible effect in your apps";
            }
            else
            {
                controller?.Restore();
                Status.Text = "Local preview · desktop unchanged";
            }
        }
        catch (Exception ex) { Fail(ex); }
        CompareButton.Content = comparing ? "Return to selected" : "Compare original";
        DrawPreview();
    }
    private void Fail(Exception ex)
    {
        lastError = ex.Message; Global.IsChecked = false;
        try { controller?.Restore(); }
        catch (Exception restore) { lastError += " Restoration failed: " + restore.Message; }
        Status.Text = "Global control paused · " + lastError;
        Notice.Text = controller?.OwnsEffect == true ? "Restoration is pending. Retry backend or Restore and exit. If Windows keeps the effect after a crash, sign out." : "Local preview remains available. Close competing color tools, check Display settings, then retry the backend.";
        QueueSave(); DrawPreview();
    }
    private void DrawPreview()
    {
        Swatches.Children.Clear();
        int preview = Global.IsChecked == true || comparing ? 100 : Percent;
        byte[][] colors = [[231, 90, 104], [239, 169, 74], [219, 218, 115], [77, 187, 146], [67, 158, 219], [151, 117, 216]];
        foreach (var c in colors)
        {
            var p = ColorMatrix.Preview(preview, c[0], c[1], c[2]);
            Swatches.Children.Add(new Border { Background = new SolidColorBrush(Color.FromRgb(p.R, p.G, p.B)), CornerRadius = new CornerRadius(6), Margin = new Thickness(3, 0, 3, 0) });
        }
        PreviewLabel.Text = Global.IsChecked == true ? "REFERENCE COLORS · VIEWED THROUGH DESKTOP EFFECT" : "LOCAL COLOR PREVIEW · ONLY THESE SWATCHES CHANGE";
    }
    private void QueueSave() { if (!ready) return; save.Stop(); save.Start(); }
    private void Save()
    {
        try { store.Save(new Settings(Percent, Global.IsChecked == true, Minimized.IsChecked == true)); }
        catch (Exception ex) { Notice.Text = "Preferences could not be saved: " + ex.Message; }
    }
    private void ResetClick(object sender, RoutedEventArgs e) { comparing = false; Amount.Value = 100; Apply(); QueueSave(); }
    private void CompareClick(object sender, RoutedEventArgs e) { comparing = !comparing; Apply(); }
    private void GlobalClick(object sender, RoutedEventArgs e) { comparing = false; Apply(); QueueSave(); }
    private void HideClick(object sender, RoutedEventArgs e) => Hide();
    private void PreferenceClick(object sender, RoutedEventArgs e) => QueueSave();
    private void StartupClick(object sender, RoutedEventArgs e)
    {
        try { StartupRegistration.Set(Startup.IsChecked == true); Notice.Text = "Startup preference updated for this executable. Keep it in a permanent folder."; }
        catch (Exception ex) { Startup.IsChecked = false; Notice.Text = "Could not update startup registration: " + ex.Message; }
    }
    private void RetryClick(object sender, RoutedEventArgs e)
    {
        try
        {
            controller?.Restore(); Global.IsChecked = false; InitializeBackend(); QueueSave(); DrawPreview();
            Status.Text = controller != null ? "Backend ready · enable desktop control to apply" : "Local preview only · " + lastError;
        }
        catch (Exception ex) { Fail(ex); }
    }
    private void DiagnosticsClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { FileName = "MK-Saturation-diagnostics.txt", Filter = "Text report|*.txt" };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            // Read on this already-initialized backend; do not initialize/uninitialize a second instance while live.
            var matrix = backend == null ? "Unavailable" : string.Join(", ", backend.Read());
            File.WriteAllText(dialog.FileName, $"MK Saturation 1.0.0\nOS: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}\nProcess: {System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}\nSelected: {Percent}%\nDesktop enabled: {Global.IsChecked}\nOwns effect: {controller?.OwnsEffect}\nMatrix: {matrix}\nLast backend error: {lastError}\nAPI readback does not prove visual coverage.\n");
            Notice.Text = "Diagnostics saved.";
        }
        catch (Exception ex) { Notice.Text = "Could not save diagnostics: " + ex.Message; }
    }
    private void DisplayClick(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("ms-settings:display") { UseShellExecute = true }); }
        catch (Exception ex) { Notice.Text = ex.Message; }
    }
    private void SessionSwitch(object sender, SessionSwitchEventArgs e)
    { if (e.Reason is SessionSwitchReason.SessionLock or SessionSwitchReason.SessionLogoff or SessionSwitchReason.RemoteDisconnect) Dispatcher.Invoke(PauseForSession); }
    private void PowerChanged(object sender, PowerModeChangedEventArgs e)
    { if (e.Mode == PowerModes.Suspend) Dispatcher.Invoke(PauseForSession); }
    private void PauseForSession()
    {
        live.Stop(); Global.IsChecked = false; Apply(); QueueSave();
        Notice.Text = "Desktop control paused after lock, disconnect or sleep. Enable it again when ready.";
    }
    public void EmergencyRestore() { try { controller?.Restore(); } catch { /* Closing reports failures; session termination is best effort. */ } }
    private void SessionEnding(object sender, SessionEndingCancelEventArgs e) => EmergencyRestore();
    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (exiting) return;
        live.Stop(); save.Stop();
        try { controller?.Restore(); }
        catch (Exception ex)
        {
            if (MessageBox.Show(this, "Windows could not confirm color restoration. " + ex.Message + "\nExit anyway? Sign out if colors remain changed.", "MK Saturation", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) { e.Cancel = true; return; }
        }
        Save(); exiting = true; monitor.Stop();
        SystemEvents.SessionSwitch -= SessionSwitch; SystemEvents.PowerModeChanged -= PowerChanged;
        System.Windows.Application.Current.SessionEnding -= SessionEnding;
        tray?.Dispose(); backend?.Dispose();
    }
}
