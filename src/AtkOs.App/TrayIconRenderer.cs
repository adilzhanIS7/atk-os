using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace AtkOs.App;

/// <summary>Draws the tray icon: battery percent on a colored badge, like G-Helper.</summary>
public static class TrayIconRenderer
{
    private const int Size = 64;

    public static WindowIcon Render(int? percent, bool charging)
    {
        var background = percent switch
        {
            null => Color.Parse("#5A5A5A"),
            <= 15 => Color.Parse("#D64545"),
            <= 30 => Color.Parse("#E0A030"),
            _ => Color.Parse("#2E9E5B"),
        };
        if (charging) background = Color.Parse("#2F7FD8");

        var text = percent is { } p ? (p >= 100 ? "99" : p.ToString(CultureInfo.InvariantCulture)) : "–";

        using var bitmap = new RenderTargetBitmap(new PixelSize(Size, Size), new Vector(96, 96));
        using (var ctx = bitmap.CreateDrawingContext())
        {
            ctx.DrawRectangle(new SolidColorBrush(background), null, new Rect(0, 0, Size, Size), 14, 14);

            var formatted = new FormattedText(
                text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface("Inter", FontStyle.Normal, FontWeight.Bold), 40, Brushes.White);
            ctx.DrawText(formatted, new Point((Size - formatted.Width) / 2, (Size - formatted.Height) / 2));
        }

        using var stream = new MemoryStream();
        bitmap.Save(stream);
        stream.Position = 0;
        return new WindowIcon(stream);
    }
}
