using System.Text.Json;

namespace AppleMusicDiscordPresence;

internal sealed class AppSettings
{
    public string DiscordApplicationId { get; set; } = "";
    public bool ShowAppleMusic { get; set; } = true;
    public bool ShowSpotify { get; set; }
    public bool ShowLyrics { get; set; } = true;
    public string EncryptedGitHubToken { get; set; } = "";
}

internal static class SettingsStore
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Mhan", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return new AppSettings();
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath)) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
    }
}
