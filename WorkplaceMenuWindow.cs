using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WinLabeler;

/// <summary>Centered list of workplace options, each in its own color, with "Manage" on top.</summary>
public sealed class WorkplaceMenuWindow : Window
{
    private bool _closing;

    public WorkplaceMenuWindow(DesktopLabelManager manager)
    {
        Title = "Workplace";
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Topmost = true;
        SizeToContent = SizeToContent.WidthAndHeight;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        var list = new StackPanel { MinWidth = 260 };

        list.Children.Add(Row("Manage", null, () =>
        {
            CloseOnce();
            manager.OpenWorkplaceManager();
        }));

        foreach (var option in manager.Settings.Workplaces)
        {
            var captured = option;
            list.Children.Add(Row(captured.Label, captured.Color, () =>
            {
                CloseOnce();
                manager.RunWorkplace(captured);
            }));
        }

        var scroll = new ScrollViewer
        {
            Content = list,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            MaxHeight = SystemParameters.WorkArea.Height * 0.7,
        };

        Content = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x1F, 0x24, 0x30)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x3A, 0x41, 0x56)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(10, 10, 10, 2),
            Child = scroll,
        };

        KeyDown += (_, e) => { if (e.Key == Key.Escape) CloseOnce(); };
        Deactivated += (_, _) => CloseOnce();   // click anywhere else dismisses the list
    }

    private void CloseOnce()
    {
        if (_closing) return;
        _closing = true;
        Close();
    }

    /// <summary>A rounded row. A null color gives the outline-only style used for "Manage".</summary>
    private static Button Row(string text, string? hex, Action onClick)
    {
        Color c = Color.FromRgb(0x3C, 0x3C, 0x3C);
        if (hex != null)
        {
            try { c = (Color)ColorConverter.ConvertFromString(hex); } catch { }
        }

        var b = WorkplaceWindow.ActionButton(text, hex == null ? Brushes.Transparent : new SolidColorBrush(c), onClick, radius: 10);
        if (hex == null)
        {
            b.BorderBrush = new SolidColorBrush(Color.FromRgb(0x8B, 0x93, 0xA7));
            b.BorderThickness = new Thickness(1.5);
            b.Foreground = Brushes.White;
        }
        else
        {
            double luminance = (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) / 255.0;
            b.Foreground = luminance > 0.6 ? Brushes.Black : Brushes.White;
        }
        b.FontSize = 15;
        b.FontWeight = FontWeights.SemiBold;
        b.Padding = new Thickness(16, 10, 16, 10);
        b.Margin = new Thickness(0, 0, 0, 8);
        b.HorizontalContentAlignment = HorizontalAlignment.Left;
        return b;
    }
}
