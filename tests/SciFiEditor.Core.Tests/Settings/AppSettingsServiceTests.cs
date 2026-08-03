using FluentAssertions;
using SciFiEditor.Core.Settings;
using SciFiEditor.Domain;

namespace SciFiEditor.Core.Tests.Settings;

public class AppSettingsServiceTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly string _filePath;

    public AppSettingsServiceTests()
    {
        _filePath = Path.Combine(_temp.Path, "settings.json");
    }

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void GetTheme_ReturnsLight_WhenFileDoesNotExist()
    {
        var service = new AppSettingsService(_filePath);

        service.GetTheme().Should().Be(AppTheme.Light);
    }

    [Fact]
    public void SetTheme_ThenGetTheme_RoundTrips()
    {
        var service = new AppSettingsService(_filePath);

        service.SetTheme(AppTheme.Dark);

        service.GetTheme().Should().Be(AppTheme.Dark);
    }

    [Fact]
    public void SetTheme_PersistsAcrossNewServiceInstances()
    {
        var service = new AppSettingsService(_filePath);
        service.SetTheme(AppTheme.Dark);

        var reloaded = new AppSettingsService(_filePath);

        reloaded.GetTheme().Should().Be(AppTheme.Dark);
    }
}
