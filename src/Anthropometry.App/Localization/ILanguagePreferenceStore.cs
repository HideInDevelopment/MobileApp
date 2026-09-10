namespace Anthropometry.App.Localization;

public interface ILanguagePreferenceStore
{
    string? GetLanguageCode();

    void SetLanguageCode(string code);
}
