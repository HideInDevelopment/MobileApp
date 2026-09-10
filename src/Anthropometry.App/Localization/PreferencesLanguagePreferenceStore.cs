using Microsoft.Maui.Storage;

namespace Anthropometry.App.Localization;

public sealed class PreferencesLanguagePreferenceStore : ILanguagePreferenceStore
{
    private const string LanguageKey = "language";

    public string? GetLanguageCode()
    {
        var code = Preferences.Default.Get(LanguageKey, string.Empty);
        return string.IsNullOrWhiteSpace(code) ? null : code;
    }

    public void SetLanguageCode(string code) => Preferences.Default.Set(LanguageKey, code);
}
