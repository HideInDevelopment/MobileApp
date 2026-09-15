using Microsoft.Maui.Storage;

namespace Anthropometry.App.Theme;

public sealed class PreferencesThemePreferenceStore : IThemePreferenceStore
{
    private const string ThemeKey = "theme";

    public string? GetThemeCode()
    {
        var code = Preferences.Default.Get(ThemeKey, string.Empty);
        return string.IsNullOrWhiteSpace(code) ? null : code;
    }

    public void SetThemeCode(string code) => Preferences.Default.Set(ThemeKey, code);
}
