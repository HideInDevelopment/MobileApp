namespace Anthropometry.App.Theme;

public interface IThemePreferenceStore
{
    string? GetThemeCode();

    void SetThemeCode(string code);
}
