using Anthropometry.App.Theme;

namespace Anthropometry.App.Tests.Localization;

public sealed class ThemeServiceTests
{
    [Fact]
    public void Initialize_defaults_to_light_when_no_theme_is_saved()
    {
        var service = new ThemeService(new FakeThemePreferenceStore());

        service.Initialize();

        Assert.Equal("light", service.CurrentThemeCode);
        Assert.Equal(ThemeMode.Light, service.CurrentTheme);
    }

    [Fact]
    public void Initialize_restores_the_saved_dark_theme()
    {
        var service = new ThemeService(new FakeThemePreferenceStore { ThemeCode = "dark" });

        service.Initialize();

        Assert.Equal("dark", service.CurrentThemeCode);
        Assert.Equal(ThemeMode.Dark, service.CurrentTheme);
    }

    [Fact]
    public void SetTheme_persists_the_selection_and_notifies_listeners()
    {
        var preferences = new FakeThemePreferenceStore();
        var service = new ThemeService(preferences);
        var changeCount = 0;
        service.ThemeChanged += (_, _) => changeCount++;
        service.Initialize();

        service.SetTheme("dark");

        Assert.Equal("dark", service.CurrentThemeCode);
        Assert.Equal(ThemeMode.Dark, service.CurrentTheme);
        Assert.Equal("dark", preferences.ThemeCode);
        Assert.Equal(2, changeCount);
    }

    [Fact]
    public void SetTheme_rejects_unsupported_theme_codes()
    {
        var service = new ThemeService(new FakeThemePreferenceStore());

        Assert.Throws<ArgumentException>(() => service.SetTheme("system"));
    }

    private sealed class FakeThemePreferenceStore : IThemePreferenceStore
    {
        public string? ThemeCode { get; set; }

        public string? GetThemeCode() => ThemeCode;

        public void SetThemeCode(string code) => ThemeCode = code;
    }
}
