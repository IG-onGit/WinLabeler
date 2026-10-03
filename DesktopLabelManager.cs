using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace WinLabeler;

/// <summary>Keeps one LabelWindow per virtual desktop, and owns the tray icon.</summary>
public sealed class DesktopLabelManager : IDisposable
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "WinLabeler";

    public AppSettings Settings { get; } = AppSettings.Load();

    private LabelWindow? _window;
    private Guid _shownId = Guid.Empty;
    private int _shownIndex;
    private readonly DispatcherTimer _timer;
    private Forms.NotifyIcon? _tray;

    public DesktopLabelManager()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _timer.Tick += (_, _) => Sync();
    }

    public void Start()
    {
        BuildTray();
        SystemEvents.DisplaySettingsChanged += OnDisplayChanged;
        Sync();
        _timer.Start();
    }

    public void Save() => Settings.Save();

    // ---------- Desktop tracking ----------

    private DesktopSettings GetSettings(Guid id)
    {
        string key = id.ToString();
        if (!Settings.Desktops.TryGetValue(key, out var s))
        {
            // If the user had a single (unidentified) desktop before, carry its label over.
            if (id != Guid.Empty && Settings.Desktops.TryGetValue(Guid.Empty.ToString(), out var legacy)
                && !Settings.Desktops.Keys.Any(k => k != Guid.Empty.ToString()))
            {
                s = legacy;
                Settings.Desktops.Remove(Guid.Empty.ToString());
            }
            else
            {
                s = new DesktopSettings();
            }
            Settings.Desktops[key] = s;
            Save();
        }
        return s;
    }

    private void Sync()
    {
        var ids = VirtualDesktops.GetDesktopIds();
        Guid? current = VirtualDesktops.GetCurrentDesktopId();

        Guid id;
        int index;
        if (ids == null || ids.Count <= 1)
        {
            id = ids is { Count: 1 } ? ids[0] : Guid.Empty;   // only one desktop exists
            index = 1;
        }
        else if (current is Guid c && ids.Contains(c))
        {
            id = c;
            index = ids.IndexOf(c) + 1;
        }
        else
        {
            return;   // current desktop momentarily unreadable: keep what is shown
        }

        if (_window == null)
        {
            _window = new LabelWindow(this, GetSettings(id));
            _window.Show();
        }

        if (id != _shownId || index != _shownIndex)
        {
            _shownId = id;
            _shownIndex = index;
            _window.Bind(GetSettings(id), index);
        }

        // Keep the single label on the desktop being viewed.
        if (id != Guid.Empty)
            VirtualDesktops.EnsureOnDesktop(_window.Handle, id);
    }

    private void OnDisplayChanged(object? sender, EventArgs e)
    {
        Application.Current.Dispatcher.BeginInvoke(new Action(() => _window?.ApplyPosition()));
    }

    // ---------- Global options ----------

    public void SetAlwaysOnTop(bool value)
    {
        Settings.AlwaysOnTop = value;
        if (_window != null) _window.Topmost = value;
        Save();
    }

    public bool IsStartupEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(RunValueName) != null;
        }
    }

    public void SetStartup(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        if (key == null) return;
        if (enabled) key.SetValue(RunValueName, $"\"{Environment.ProcessPath}\"");
        else key.DeleteValue(RunValueName, throwOnMissingValue: false);
    }

    // ---------- Workplace ----------

    private WorkplaceWindow? _workplaceWindow;

    public void OpenWorkplaceManager()
    {
        if (_workplaceWindow == null)
        {
            _workplaceWindow = new WorkplaceWindow(this);
            _workplaceWindow.Closed += (_, _) => _workplaceWindow = null;
            _workplaceWindow.Show();
        }
        if (_workplaceWindow.WindowState == WindowState.Minimized)
            _workplaceWindow.WindowState = WindowState.Normal;
        _workplaceWindow.Activate();
    }

    /// <summary>Runs the option's commands through cmd.exe (hidden console window).</summary>
    public void RunWorkplace(WorkplaceOption option)
    {
        if (string.IsNullOrWhiteSpace(option.Command)) return;
        try
        {
            // A temp batch file lets multi-line commands work as written.
            string file = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"winlabeler-{Guid.NewGuid():N}.cmd");
            System.IO.File.WriteAllText(file, BuildBatch(option.Command));
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"\"{file}\"\"",
                UseShellExecute = false,
                CreateNoWindow = true,
            });

            // Label the desktop being viewed with the workplace that was just launched.
            var desktop = GetSettings(_shownId);
            desktop.Label = option.Label;
            desktop.Color = option.Color;
            Save();
            _window?.Bind(desktop, _shownIndex);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, $"Workplace: {option.Label}", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static readonly string[] NoCallPrefixes = { ":", "@", "rem ", "if ", "for ", "goto ", "(", ")" };

    /// <summary>
    /// One line per command. Tools like `code` are .cmd files, which end a batch script
    /// unless started with `call`, so each plain command line gets that prefix.
    /// </summary>
    private static string BuildBatch(string commands)
    {
        var sb = new System.Text.StringBuilder("@echo off\r\n");
        foreach (var raw in commands.Replace("\r\n", "\n").Split('\n'))
        {
            string line = raw.Trim();
            if (line.Length == 0) continue;
            bool plain = !NoCallPrefixes.Any(p => line.StartsWith(p, StringComparison.OrdinalIgnoreCase));
            sb.Append(plain ? "call " : "").Append(line).Append("\r\n");
        }
        sb.Append("del \"%~f0\" >nul 2>nul\r\n");
        return sb.ToString();
    }

    public void Exit()
    {
        Dispose();
        Application.Current.Shutdown();
    }

    // ---------- Tray icon ----------

    private void BuildTray()
    {
        var menu = new Forms.ContextMenuStrip();

        var onTop = new Forms.ToolStripMenuItem("Always on top") { CheckOnClick = true };
        onTop.Click += (_, _) => SetAlwaysOnTop(onTop.Checked);

        var startup = new Forms.ToolStripMenuItem("Start with Windows") { CheckOnClick = true };
        startup.Click += (_, _) => SetStartup(startup.Checked);

        var exit = new Forms.ToolStripMenuItem("Exit");
        exit.Click += (_, _) => Exit();

        menu.Items.Add(onTop);
        menu.Items.Add(startup);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(exit);
        menu.Opening += (_, _) =>
        {
            onTop.Checked = Settings.AlwaysOnTop;
            startup.Checked = IsStartupEnabled;
        };

        _tray = new Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Text = "WinLabeler",
            Visible = true,
            ContextMenuStrip = menu,
        };
    }

    public void Dispose()
    {
        _timer.Stop();
        SystemEvents.DisplaySettingsChanged -= OnDisplayChanged;

        _window?.Close();
        _window = null;

        if (_tray != null)
        {
            _tray.Visible = false;
            _tray.Dispose();
            _tray = null;
        }
    }
}
