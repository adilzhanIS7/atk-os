using System.Text.Json;
using System.Text.Json.Serialization;

namespace AtkOs.App.Services;

public sealed class AppSettings
{
    public bool LowBatteryAlert { get; set; } = true;
    public int LowBatteryThreshold { get; set; } = 20;
}

[JsonSerializable(typeof(AppSettings))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal partial class SettingsJsonContext : JsonSerializerContext;

/// <summary>App settings in the user's config folder (e.g. %APPDATA%\atk-os\settings.json).</summary>
public static class SettingsStore
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "atk-os", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize(File.ReadAllText(FilePath), SettingsJsonContext.Default.AppSettings)
                       ?? new AppSettings();
        }
        catch
        {
            // Broken file: fall back to defaults.
        }
        return new AppSettings();
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, SettingsJsonContext.Default.AppSettings));
        }
        catch
        {
            // Not critical: settings just won't persist.
        }
    }
}
