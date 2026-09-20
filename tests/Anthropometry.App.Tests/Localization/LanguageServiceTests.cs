using Anthropometry.App.Localization;

namespace Anthropometry.App.Tests.Localization;

public sealed class LanguageServiceTests
{
    [Fact]
    public void Initialize_defaults_to_english_when_no_language_is_saved()
    {
        var preferences = new FakeLanguagePreferenceStore();
        var service = new LanguageService(preferences);

        service.Initialize();

        Assert.Equal("en", service.CurrentLanguageCode);
        Assert.Equal("Settings", service.Get("SettingsTitle"));
    }

    [Fact]
    public void Initialize_restores_the_saved_language()
    {
        var preferences = new FakeLanguagePreferenceStore { LanguageCode = "de" };
        var service = new LanguageService(preferences);

        service.Initialize();

        Assert.Equal("de", service.CurrentLanguageCode);
        Assert.Equal("Einstellungen", service.Get("SettingsTitle"));
    }

    [Fact]
    public void SetLanguage_persists_the_selection_and_changes_translations()
    {
        var preferences = new FakeLanguagePreferenceStore();
        var service = new LanguageService(preferences);
        service.Initialize();

        service.SetLanguage("es");

        Assert.Equal("es", service.CurrentLanguageCode);
        Assert.Equal("Configuración", service.Get("SettingsTitle"));
        Assert.Equal("es", preferences.LanguageCode);
    }

    [Fact]
    public void SetLanguage_rejects_unsupported_language_codes()
    {
        var service = new LanguageService(new FakeLanguagePreferenceStore());

        Assert.Throws<ArgumentException>(() => service.SetLanguage("fr"));
    }

    [Fact]
    public void Protected_transfer_copy_is_available_in_english_spanish_and_german()
    {
        var service = new LanguageService(new FakeLanguagePreferenceStore());
        var keys = new[]
        {
            "ProfileTransferPassphraseTitle",
            "ProfileTransferPassphraseInstructions",
            "ProfileTransferPassphrasePlaceholder",
            "ProfileTransferPassphraseConfirmationPlaceholder",
            "ProfileTransferPassphraseInvalid",
            "ProfileTransferPassphraseMismatch",
            "ProfileTransferPasswordRequired",
            "ProfileTransferAuthenticationFailed",
            "ProfileTransferLegacyWarning"
        };

        service.Initialize();
        foreach (var language in new[] { "en", "es", "de" })
        {
            service.SetLanguage(language);
            Assert.All(keys, key => Assert.NotEqual(key, service.Get(key)));
        }
    }

    private sealed class FakeLanguagePreferenceStore : ILanguagePreferenceStore
    {
        public string? LanguageCode { get; set; }

        public string? GetLanguageCode() => LanguageCode;

        public void SetLanguageCode(string code) => LanguageCode = code;
    }
}
