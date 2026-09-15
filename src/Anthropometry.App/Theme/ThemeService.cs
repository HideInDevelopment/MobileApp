namespace Anthropometry.App.Theme;

public enum ThemeMode
{
    Light,
    Dark
}

public sealed class ThemeService
{
    public const string LightCode = "light";
    public const string DarkCode = "dark";

    private static readonly ThemeDefinition[] SupportedThemes =
    [
        new(LightCode, ThemeMode.Light),
        new(DarkCode, ThemeMode.Dark)
    ];

    private readonly IThemePreferenceStore _preferences;

    public ThemeService(IThemePreferenceStore preferences)
    {
        _preferences = preferences;
    }

    public event EventHandler? ThemeChanged;

    public string CurrentThemeCode { get; private set; } = LightCode;

    public ThemeMode CurrentTheme { get; private set; } = ThemeMode.Light;

    public void Initialize()
    {
        var savedCode = _preferences.GetThemeCode();
        var theme = SupportedThemes.FirstOrDefault(candidate =>
            string.Equals(candidate.Code, savedCode, StringComparison.OrdinalIgnoreCase))
            ?? SupportedThemes[0];
        Apply(theme, persist: false);
    }

    public void SetTheme(string code)
    {
        var theme = SupportedThemes.FirstOrDefault(candidate =>
            string.Equals(candidate.Code, code, StringComparison.OrdinalIgnoreCase));
        if (theme is null)
        {
            throw new ArgumentException($"Unsupported theme code: {code}", nameof(code));
        }

        Apply(theme, persist: true);
    }

    private void Apply(ThemeDefinition theme, bool persist)
    {
        CurrentThemeCode = theme.Code;
        CurrentTheme = theme.Mode;
        if (persist)
        {
            _preferences.SetThemeCode(theme.Code);
        }

        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    private sealed record ThemeDefinition(string Code, ThemeMode Mode);
}
