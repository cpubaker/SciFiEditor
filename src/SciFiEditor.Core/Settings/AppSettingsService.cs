using System.Text.Json;
using SciFiEditor.Domain;

namespace SciFiEditor.Core.Settings;

public sealed class AppSettingsService
{
    private readonly string _filePath;

    public AppSettingsService()
        : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SciFiEditor", "settings.json"))
    {
    }

    public AppSettingsService(string filePath)
    {
        _filePath = filePath;
    }

    public AppTheme GetTheme()
    {
        if (!File.Exists(_filePath))
        {
            return AppTheme.Light;
        }

        var json = File.ReadAllText(_filePath);
        var settings = JsonSerializer.Deserialize<AppSettingsData>(json);
        return settings?.Theme ?? AppTheme.Light;
    }

    public void SetTheme(AppTheme theme)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(new AppSettingsData(theme), new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }

    private sealed record AppSettingsData(AppTheme Theme);
}
