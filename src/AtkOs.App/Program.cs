using Avalonia;
using Avalonia.Controls;

namespace AtkOs.App;

internal static class Program
{
    /// <summary>Start with "--tray" to stay hidden in the tray (for autostart).</summary>
    public static bool StartInTray { get; private set; }

    /// <summary>"--test-notification" shows the low-battery popup right away.</summary>
    public static bool TestNotification { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        StartInTray = args.Contains("--tray");
        TestNotification = args.Contains("--test-notification");
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, ShutdownMode.OnExplicitShutdown);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
