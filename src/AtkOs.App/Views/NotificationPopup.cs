using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace AtkOs.App.Views;

/// <summary>
/// Small toast in the bottom-right corner that closes by itself.
/// Our own window instead of OS notifications, so it looks the same everywhere.
/// </summary>
public sealed class NotificationPopup : Window
{
    private static NotificationPopup? _current;

    private NotificationPopup(string title, string message, Color accent)
    {
        SystemDecorations = SystemDecorations.None;
        Topmost = true;
        ShowActivated = false;
        ShowInTaskbar = false;
        CanResize = false;
        SizeToContent = SizeToContent.WidthAndHeight;
        Background = Brushes.Transparent;
        TransparencyLevelHint = [WindowTransparencyLevel.Transparent];

        Content = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#1E1F22")),
            BorderBrush = new SolidColorBrush(accent),
            BorderThickness = new Thickness(4, 0, 0, 0),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16, 12),
            Width = 300,
            Child = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = title, FontWeight = FontWeight.SemiBold, FontSize = 14, Foreground = Brushes.White },
                    new TextBlock
                    {
                        Text = message, FontSize = 12, TextWrapping = TextWrapping.Wrap,
                        Foreground = new SolidColorBrush(Color.Parse("#9DA3AE")),
                    },
                },
            },
        };

        PointerPressed += (_, _) => Close();
        // The size is known only after the first layout pass, so re-place on every size change.
        SizeChanged += (_, e) => PlaceInCorner(e.NewSize);
    }

    public static void Show(string title, string message, Color accent, int seconds = 8)
    {
        _current?.Close();
        var popup = _current = new NotificationPopup(title, message, accent);
        popup.Closed += (_, _) =>
        {
            if (_current == popup) _current = null;
            MemoryTrimmer.Trim();
        };
        popup.Show();
        DispatcherTimer.RunOnce(() => popup.Close(), TimeSpan.FromSeconds(seconds));
    }

    private void PlaceInCorner(Size clientSize)
    {
        if (clientSize.Width <= 0 || clientSize.Height <= 0) return;
        var screen = Screens.Primary ?? Screens.ScreenFromWindow(this);
        if (screen == null) return;
        var area = screen.WorkingArea;
        var scale = screen.Scaling;
        var size = PixelSize.FromSize(clientSize, scale);
        var margin = (int)(16 * scale);
        Position = new PixelPoint(area.Right - size.Width - margin, area.Bottom - size.Height - margin);
    }
}
