using Microsoft.Win32;

namespace AtkOs.App.Services;

/// <summary>
/// Start the app (in the tray) when the user logs in.
/// Windows: HKCU Run key. Linux: XDG autostart .desktop file. macOS: LaunchAgent.
/// </summary>
public static class Autostart
{
    private const string Name = "ATK-OS";
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    private static string Home => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    private static string LinuxFile => Path.Combine(
        Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") is { Length: > 0 } x ? x : Path.Combine(Home, ".config"),
        "autostart", "atk-os.desktop");
    private static string MacFile => Path.Combine(Home, "Library", "LaunchAgents", "io.github.atk-os.plist");

    private static string ExePath => Environment.ProcessPath ?? throw new InvalidOperationException("Unknown executable path");

    public static bool IsSupported =>
        OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS();

    public static bool IsEnabled()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKey);
                return key?.GetValue(Name) is string;
            }
            if (OperatingSystem.IsLinux()) return File.Exists(LinuxFile);
            if (OperatingSystem.IsMacOS()) return File.Exists(MacFile);
        }
        catch
        {
            // Treat unreadable as disabled.
        }
        return false;
    }

    public static void SetEnabled(bool enabled)
    {
        if (OperatingSystem.IsWindows())
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey);
            if (enabled) key.SetValue(Name, $"\"{ExePath}\" --tray");
            else key.DeleteValue(Name, throwOnMissingValue: false);
        }
        else if (OperatingSystem.IsLinux())
        {
            if (enabled)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LinuxFile)!);
                File.WriteAllText(LinuxFile, $"""
                    [Desktop Entry]
                    Type=Application
                    Name=ATK-OS
                    Exec="{ExePath}" --tray
                    X-GNOME-Autostart-enabled=true
                    """);
            }
            else File.Delete(LinuxFile);
        }
        else if (OperatingSystem.IsMacOS())
        {
            if (enabled)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(MacFile)!);
                File.WriteAllText(MacFile, $"""
                    <?xml version="1.0" encoding="UTF-8"?>
                    <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
                    <plist version="1.0">
                    <dict>
                      <key>Label</key><string>io.github.atk-os</string>
                      <key>ProgramArguments</key>
                      <array><string>{ExePath}</string><string>--tray</string></array>
                      <key>RunAtLoad</key><true/>
                    </dict>
                    </plist>
                    """);
            }
            else File.Delete(MacFile);
        }
    }
}
