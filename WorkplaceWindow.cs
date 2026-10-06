using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WinLabeler;

/// <summary>Window for creating, updating and deleting workplace options.</summary>
public sealed class WorkplaceWindow : Window
{
    private static readonly Brush Bg = MakeBrush("#1F2430");
    private static readonly Brush HeaderBg = MakeBrush("#171B24");
    private static readonly Brush Field = MakeBrush("#2A3040");
    private static readonly Brush Accent = MakeBrush("#2D6CDF");
    private static readonly Brush Danger = MakeBrush("#D64545");
    private static readonly Brush Text = MakeBrush("#E6E9F0");
    private static readonly Brush Muted = MakeBrush("#8B93A7");

    private readonly DesktopLabelManager _manager;
    private readonly ListBox _list;
    private readonly TextBox _label;
    private readonly TextBox _command;
    private readonly Button _save;
    private readonly Button _delete;
    private readonly TextBlock _status;
    private readonly WrapPanel _swatches = new() { Margin = new Thickness(0, 6, 0, 0) };
    private WorkplaceOption? _selected;   // null = creating a new option
    private string _color = DefaultColor;
    private const string DefaultColor = "#2D6CDF";

    public WorkplaceWindow(DesktopLabelManager manager)
    {
        _manager = manager;

        Title = "Workplace";
        Width = 760;
        Height = 480;
        MinWidth = 600;
        MinHeight = 380;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.CanResizeWithGrip;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = Bg;
        Foreground = Text;

        // ----- header -----
        var title = new TextBlock
        {
            Text = "Workplace",
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = Text,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(14, 0, 0, 0),
        };
        var minimize = HeaderButton("—", MakeBrush("#2A3040"), () => WindowState = WindowState.Minimized);
        var close = HeaderButton("✕", Danger, Close);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal };
        buttons.Children.Add(minimize);
        buttons.Children.Add(close);

        var header = new Grid { Background = HeaderBg, Height = 38 };
        header.Children.Add(title);
        header.Children.Add(buttons);
        buttons.HorizontalAlignment = HorizontalAlignment.Right;
        header.MouseLeftButtonDown += (_, e) =>
        {
            if (e.ClickCount == 1) DragMove();
        };

        // ----- left: list -----
        _list = new ListBox
        {
            Background = Field,
            Foreground = Text,
            BorderThickness = new Thickness(0),
            DisplayMemberPath = nameof(WorkplaceOption.Label),
            Margin = new Thickness(0, 6, 0, 8),
        };
        _list.SelectionChanged += (_, _) =>
        {
            if (_list.SelectedItem is WorkplaceOption o) Select(o);
        };

        var newBtn = ActionButton("+ New option", Accent, NewOption);
        var left = new DockPanel { Margin = new Thickness(14, 12, 7, 14), Width = 220 };
        var leftTitle = Caption("OPTIONS");
        DockPanel.SetDock(leftTitle, Dock.Top);
        DockPanel.SetDock(newBtn, Dock.Bottom);
        left.Children.Add(leftTitle);
        left.Children.Add(newBtn);
        left.Children.Add(_list);

        // ----- right: editor -----
        _label = new TextBox { Style = null, Background = Field, Foreground = Text, CaretBrush = Text,
            BorderThickness = new Thickness(0), Padding = new Thickness(8, 6, 8, 6), FontSize = 13 };
        _command = new TextBox
        {
            Style = null,
            Background = Field,
            Foreground = Text,
            CaretBrush = Text,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(8, 6, 8, 6),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 13,
            AcceptsReturn = true,
            AcceptsTab = true,
            TextWrapping = TextWrapping.NoWrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
        };

        _save = ActionButton("Save", Accent, Save);
        _delete = ActionButton("Delete", Danger, Delete);
        _status = new TextBlock { Foreground = Muted, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 0, 0) };

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0) };
        actions.Children.Add(_save);
        actions.Children.Add(_delete);
        actions.Children.Add(_status);

        var labelCaption = Caption("LABEL");
        var cmdCaption = Caption("COMMAND LINE CODE");
        cmdCaption.Margin = new Thickness(0, 10, 0, 0);
        _label.Margin = new Thickness(0, 6, 0, 0);
        _command.Margin = new Thickness(0, 6, 0, 0);

        var colorCaption = Caption("LABEL COLOR");
        colorCaption.Margin = new Thickness(0, 10, 0, 0);

        var right = new DockPanel { Margin = new Thickness(7, 12, 14, 14) };
        DockPanel.SetDock(labelCaption, Dock.Top);
        DockPanel.SetDock(_label, Dock.Top);
        DockPanel.SetDock(colorCaption, Dock.Top);
        DockPanel.SetDock(_swatches, Dock.Top);
        DockPanel.SetDock(cmdCaption, Dock.Top);
        DockPanel.SetDock(actions, Dock.Bottom);
        right.Children.Add(labelCaption);
        right.Children.Add(_label);
        right.Children.Add(colorCaption);
        right.Children.Add(_swatches);
        right.Children.Add(cmdCaption);
        right.Children.Add(actions);
        right.Children.Add(_command);

        var body = new Grid();
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(left, 0);
        Grid.SetColumn(right, 1);
        body.Children.Add(left);
        body.Children.Add(right);

        var root = new DockPanel();
        DockPanel.SetDock(header, Dock.Top);
        root.Children.Add(header);
        root.Children.Add(body);

        Content = new Border { BorderBrush = MakeBrush("#3A4156"), BorderThickness = new Thickness(1), Child = root };

        Reload();
        NewOption();
    }

    private void Reload()
    {
        _list.ItemsSource = null;
        _list.ItemsSource = _manager.Settings.Workplaces;
    }

    private void Select(WorkplaceOption o)
    {
        _selected = o;
        _label.Text = o.Label;
        _command.Text = o.Command;
        SetColor(o.Color);
        _delete.IsEnabled = true;
        _status.Text = "";
    }

    private void NewOption()
    {
        _selected = null;
        _list.SelectedItem = null;
        _label.Text = "";
        _command.Text = "";
        SetColor(DefaultColor);
        _delete.IsEnabled = false;
        _status.Text = "New option";
        _label.Focus();
    }

    private void Save()
    {
        string label = _label.Text.Trim();
        if (label.Length == 0)
        {
            _status.Text = "Enter a label first.";
            return;
        }

        if (_selected == null)
        {
            _selected = new WorkplaceOption();
            _manager.Settings.Workplaces.Add(_selected);
        }
        _selected.Label = label;
        _selected.Command = _command.Text;
        _selected.Color = _color;
        _manager.Save();

        Reload();
        _list.SelectedItem = _selected;
        _status.Text = "Saved.";
    }

    private void Delete()
    {
        if (_selected == null) return;
        _manager.Settings.Workplaces.Remove(_selected);
        _manager.Save();
        Reload();
        NewOption();
        _status.Text = "Deleted.";
    }

    // ---------- Color ----------

    private void SetColor(string hex)
    {
        _color = hex;
        _swatches.Children.Clear();

        bool custom = true;
        foreach (var (name, value) in LabelWindow.Palette)
        {
            bool active = string.Equals(value, hex, StringComparison.OrdinalIgnoreCase);
            custom &= !active;
            string captured = value;
            _swatches.Children.Add(Swatch(value, name, active, () => SetColor(captured)));
        }

        // Last swatch: shows the custom color when one is set, otherwise a "+" to pick one.
        var picker = Swatch(custom ? hex : "#2A3040", "Custom…", custom, PickCustomColor);
        if (!custom) ((Border)picker).Child = new TextBlock
        {
            Text = "+", Foreground = Muted, HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center, FontSize = 14,
        };
        _swatches.Children.Add(picker);
    }

    private static UIElement Swatch(string hex, string tip, bool active, Action onClick)
    {
        var b = new Border
        {
            Width = 22,
            Height = 22,
            CornerRadius = new CornerRadius(11),
            Margin = new Thickness(0, 0, 8, 0),
            Background = MakeBrush(hex),
            BorderBrush = active ? Brushes.White : Brushes.Transparent,
            BorderThickness = new Thickness(2),
            Cursor = Cursors.Hand,
            ToolTip = tip,
        };
        b.MouseLeftButtonUp += (_, _) => onClick();
        return b;
    }

    private void PickCustomColor()
    {
        var c = (Color)ColorConverter.ConvertFromString(_color);
        using var dlg = new System.Windows.Forms.ColorDialog
        {
            FullOpen = true,
            Color = System.Drawing.Color.FromArgb(c.R, c.G, c.B),
        };
        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            SetColor($"#{dlg.Color.R:X2}{dlg.Color.G:X2}{dlg.Color.B:X2}");
    }

    // ---------- UI helpers ----------

    private static Brush MakeBrush(string hex)
    {
        var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        b.Freeze();
        return b;
    }

    private static TextBlock Caption(string text) => new()
    {
        Text = text,
        FontSize = 11,
        FontWeight = FontWeights.SemiBold,
        Foreground = Muted,
    };

    internal static Button ActionButton(string text, Brush bg, Action onClick, double radius = 0)
    {
        var b = new Button
        {
            Content = text,
            Background = bg,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(16, 7, 16, 7),
            Margin = new Thickness(0, 0, 8, 0),
            Cursor = Cursors.Hand,
            Template = ActionButtonTemplate(radius),
        };
        b.Click += (_, _) => onClick();
        return b;
    }

    /// <summary>Flat button: slight dim on hover, dark blue when disabled.</summary>
    private static ControlTemplate ActionButtonTemplate(double radius)
    {
        var template = new ControlTemplate(typeof(Button));
        var border = new FrameworkElementFactory(typeof(Border), "bd");
        border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(BackgroundProperty));
        border.SetValue(Border.PaddingProperty, new TemplateBindingExtension(PaddingProperty));
        border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(BorderBrushProperty));
        border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(BorderThicknessProperty));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(radius));
        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
        presenter.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
        border.AppendChild(presenter);
        template.VisualTree = border;

        var hover = new Trigger { Property = IsMouseOverProperty, Value = true };
        hover.Setters.Add(new Setter(OpacityProperty, 0.85, "bd"));
        template.Triggers.Add(hover);

        var disabled = new Trigger { Property = IsEnabledProperty, Value = false };
        disabled.Setters.Add(new Setter(Border.BackgroundProperty, MakeBrush("#26324D"), "bd"));
        disabled.Setters.Add(new Setter(ForegroundProperty, Muted));
        template.Triggers.Add(disabled);
        return template;
    }

    private static Button HeaderButton(string glyph, Brush hover, Action onClick)
    {
        var b = new Button
        {
            Content = glyph,
            Width = 46,
            Background = Brushes.Transparent,
            Foreground = Text,
            BorderThickness = new Thickness(0),
            FontSize = 13,
            Cursor = Cursors.Hand,
            Template = HeaderButtonTemplate(hover),
        };
        b.Click += (_, _) => onClick();
        return b;
    }

    private static ControlTemplate HeaderButtonTemplate(Brush hover)
    {
        var template = new ControlTemplate(typeof(Button));
        var border = new FrameworkElementFactory(typeof(Border), "bd");
        border.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
        presenter.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
        border.AppendChild(presenter);
        template.VisualTree = border;
        var trigger = new Trigger { Property = IsMouseOverProperty, Value = true };
        trigger.Setters.Add(new Setter(Border.BackgroundProperty, hover, "bd"));
        template.Triggers.Add(trigger);
        return template;
    }
}
