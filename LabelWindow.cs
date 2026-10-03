using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace WinLabeler;

/// <summary>The small rounded label shown on one virtual desktop.</summary>
public sealed class LabelWindow : Window
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const double EdgeMargin = 12;

    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    internal static readonly (string Name, string Hex)[] Palette =
    {
        ("Blue", "#2D6CDF"), ("Green", "#2E9E5B"), ("Red", "#D64545"), ("Orange", "#E8892B"),
        ("Yellow", "#F2C94C"), ("Purple", "#8A5CF5"), ("Teal", "#1FA2A6"), ("Pink", "#D6579B"),
        ("Slate", "#475569"), ("Black", "#1F1F1F"),
    };

    private readonly DesktopLabelManager _manager;
    private DesktopSettings _s;
    private readonly Border _border;
    private readonly TextBlock _text;
    private readonly TextBox _edit;
    private bool _editing;
    private bool _dragging;
    private int _index = 1;

    public LabelWindow(DesktopLabelManager manager, DesktopSettings settings)
    {
        _manager = manager;
        _s = settings;

        Title = "Desktop label";
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        ShowActivated = false;
        SizeToContent = SizeToContent.WidthAndHeight;
        Topmost = manager.Settings.AlwaysOnTop;

        _text = new TextBlock
        {
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 320,
        };

        _edit = new TextBox
        {
            Visibility = Visibility.Collapsed,
            MinWidth = 140,
            MaxLength = 40,
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var grid = new Grid();
        grid.Children.Add(_text);
        grid.Children.Add(_edit);

        _border = new Border
        {
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16, 8, 16, 8),
            Child = grid,
            Cursor = Cursors.SizeAll,
        };
        Content = _border;

        _border.ContextMenu = BuildMenu();
        _border.MouseLeftButtonDown += OnMouseDown;

        _edit.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter) { EndEdit(true); e.Handled = true; }
            else if (e.Key == Key.Escape) { EndEdit(false); e.Handled = true; }
        };
        _edit.LostKeyboardFocus += (_, _) => EndEdit(true);

        SourceInitialized += (_, _) =>
        {
            // Tool window: hidden from Alt+Tab.
            var h = new WindowInteropHelper(this).Handle;
            SetWindowLong(h, GWL_EXSTYLE, GetWindowLong(h, GWL_EXSTYLE) | WS_EX_TOOLWINDOW);
        };
        SizeChanged += (_, _) => ApplyPosition();

        ApplyColor();
        RefreshText();
    }

    public IntPtr Handle => new WindowInteropHelper(this).EnsureHandle();

    public int Index
    {
        get => _index;
        set { _index = value; RefreshText(); }
    }

    /// <summary>Switches this window to show another desktop's label.</summary>
    public void Bind(DesktopSettings settings, int index)
    {
        EndEdit(true);
        _s = settings;
        _index = index;
        ApplyColor();
        RefreshText();
        ApplyPosition();
    }

    private string DisplayLabel =>
        string.IsNullOrWhiteSpace(_s.Label) ? $"Desktop {_index}" : _s.Label;

    private void RefreshText() => _text.Text = DisplayLabel;

    // ---------- Appearance ----------

    private static Color ParseColor(string hex)
    {
        try { return (Color)ColorConverter.ConvertFromString(hex); }
        catch { return Color.FromRgb(0x2D, 0x6C, 0xDF); }
    }

    private void ApplyColor()
    {
        var c = ParseColor(_s.Color);
        _border.Background = new SolidColorBrush(c);

        double luminance = (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) / 255.0;
        Brush fg = luminance > 0.6 ? Brushes.Black : Brushes.White;
        _text.Foreground = fg;
        _edit.Foreground = fg;
        _edit.CaretBrush = fg;
    }

    private void SetColor(string hex)
    {
        _s.Color = hex;
        ApplyColor();
        _manager.Save();
    }

    private void PickCustomColor()
    {
        var current = ParseColor(_s.Color);
        using var dlg = new System.Windows.Forms.ColorDialog
        {
            FullOpen = true,
            Color = System.Drawing.Color.FromArgb(current.R, current.G, current.B),
        };
        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            SetColor($"#{dlg.Color.R:X2}{dlg.Color.G:X2}{dlg.Color.B:X2}");
    }

    // ---------- Position ----------

    public void ApplyPosition()
    {
        if (_dragging) return;
        var wa = SystemParameters.WorkArea;
        double w = ActualWidth, h = ActualHeight;
        if (w <= 0 || h <= 0) return;

        bool left = _s.Corner is Corner.BottomLeft or Corner.TopLeft;
        bool center = _s.Corner is Corner.BottomCenter or Corner.TopCenter;
        bool top = _s.Corner is Corner.TopLeft or Corner.TopRight or Corner.TopCenter;

        Left = center ? wa.Left + (wa.Width - w) / 2
             : left ? wa.Left + EdgeMargin
             : wa.Right - w - EdgeMargin;
        Top = top ? wa.Top + EdgeMargin : wa.Bottom - h - EdgeMargin;
    }

    private void SetCorner(Corner corner)
    {
        _s.Corner = corner;
        _manager.Save();
        ApplyPosition();
    }

    private void SnapToNearestCorner()
    {
        var wa = SystemParameters.WorkArea;
        double cx = Left + ActualWidth / 2;
        double cy = Top + ActualHeight / 2;
        bool bottom = cy > wa.Top + wa.Height / 2;

        // Screen split into thirds: left, middle (centered), right.
        double third = wa.Width / 3;
        int column = cx < wa.Left + third ? 0 : cx < wa.Left + 2 * third ? 1 : 2;

        _s.Corner = (column, bottom) switch
        {
            (0, true) => Corner.BottomLeft,
            (2, true) => Corner.BottomRight,
            (1, true) => Corner.BottomCenter,
            (0, false) => Corner.TopLeft,
            (1, false) => Corner.TopCenter,
            _ => Corner.TopRight,
        };
        _manager.Save();
        ApplyPosition();
    }

    // ---------- Interaction ----------

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_editing) return;

        if (e.ClickCount == 2)
        {
            BeginEdit();
            e.Handled = true;
            return;
        }

        _dragging = true;
        try { DragMove(); } catch (InvalidOperationException) { }
        _dragging = false;
        SnapToNearestCorner();
    }

    private void BeginEdit()
    {
        if (_editing) return;
        _editing = true;
        _edit.Text = _s.Label.Length > 0 ? _s.Label : DisplayLabel;
        _text.Visibility = Visibility.Collapsed;
        _edit.Visibility = Visibility.Visible;

        Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
        {
            Activate();
            _edit.Focus();
            Keyboard.Focus(_edit);
            _edit.SelectAll();
        }));
    }

    private void EndEdit(bool commit)
    {
        if (!_editing) return;
        _editing = false;

        if (commit)
        {
            _s.Label = _edit.Text.Trim();   // empty -> falls back to "Desktop N"
            _manager.Save();
        }

        _edit.Visibility = Visibility.Collapsed;
        _text.Visibility = Visibility.Visible;
        RefreshText();
    }

    // ---------- Context menu ----------

    private static MenuItem Item(string header, Action onClick)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) => onClick();
        return item;
    }

    private ContextMenu BuildMenu()
    {
        var menu = new ContextMenu();

        var colors = new MenuItem { Header = "Color" };
        foreach (var (name, hex) in Palette)
        {
            var swatch = new Border
            {
                Width = 14,
                Height = 14,
                CornerRadius = new CornerRadius(7),
                Background = new SolidColorBrush(ParseColor(hex)),
            };
            var mi = new MenuItem { Header = name, Icon = swatch };
            string captured = hex;
            mi.Click += (_, _) => SetColor(captured);
            colors.Items.Add(mi);
        }
        colors.Items.Add(new Separator());
        colors.Items.Add(Item("Custom…", PickCustomColor));

        var onTop = new MenuItem { Header = "Always on top", IsCheckable = true };
        onTop.Click += (_, _) => _manager.SetAlwaysOnTop(onTop.IsChecked);

        var startup = new MenuItem { Header = "Start with Windows", IsCheckable = true };
        startup.Click += (_, _) => _manager.SetStartup(startup.IsChecked);

        var settings = new MenuItem { Header = "Settings" };
        settings.Items.Add(colors);
        settings.Items.Add(onTop);
        settings.Items.Add(startup);
        settings.Items.Add(Item("Exit", _manager.Exit));

        // Workplace options come from the user's setup, so the menu is rebuilt each time it opens.
        menu.Opened += (_, _) =>
        {
            onTop.IsChecked = _manager.Settings.AlwaysOnTop;
            startup.IsChecked = _manager.IsStartupEnabled;

            menu.Items.Clear();
            menu.Items.Add(Item("Manage", _manager.OpenWorkplaceManager));
            if (_manager.Settings.Workplaces.Count > 0) menu.Items.Add(new Separator());
            foreach (var option in _manager.Settings.Workplaces)
            {
                var captured = option;
                menu.Items.Add(Item(captured.Label, () => _manager.RunWorkplace(captured)));
            }
            menu.Items.Add(new Separator());
            menu.Items.Add(settings);
        };

        return menu;
    }
}
