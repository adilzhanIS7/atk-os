using Avalonia;
using Avalonia.Controls;

namespace AtkOs.App;

internal static class Program
{
    /// <summary>Start with "--tray" to stay hidden in the tray (for autostart).</summary>
    public static bool StartInTray { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        StartInTray = args.Contains("--tray");
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, ShutdownMode.OnExplicitShutdown);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
