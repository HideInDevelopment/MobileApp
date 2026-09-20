using System.Globalization;
using Anthropometry.App.Localization;

namespace Anthropometry.App.Tests.Localization;

public sealed class LanguageServiceTests
{
    [Fact]
    public void Initialize_defaults_to_english_when_no_language_is_saved()
    {
        var preferences = new FakeLanguagePreferenceStore();
        var service = new LanguageService(preferences);

        service.Initialize(CultureInfo.GetCultureInfo("en-US"));

        Assert.Equal("en", service.CurrentLanguageCode);
        Assert.Equal("Settings", service.Get("SettingsTitle"));
    }

    [Fact]
    public void Initialize_restores_the_saved_language()
    {
        var preferences = new FakeLanguagePreferenceStore { LanguageCode = "de" };
        var service = new LanguageService(preferences);

        service.Initialize(CultureInfo.GetCultureInfo("en-US"));

        Assert.Equal("de", service.CurrentLanguageCode);
        Assert.Equal("Einstellungen", service.Get("SettingsTitle"));
    }

    [Fact]
    public void SetLanguage_persists_the_selection_and_changes_translations()
    {
        var preferences = new FakeLanguagePreferenceStore();
        var service = new LanguageService(preferences);
        service.Initialize(CultureInfo.GetCultureInfo("en-US"));

        service.SetLanguage("es");

        Assert.Equal("es", service.CurrentLanguageCode);
        Assert.Equal("Configuración", service.Get("SettingsTitle"));
        Assert.Equal("es", preferences.LanguageCode);
    }

    [Fact]
    public void SetLanguage_rejects_unsupported_language_codes()
    {
        var service = new LanguageService(new FakeLanguagePreferenceStore());

        Assert.Throws<ArgumentException>(() => service.SetLanguage("xx"));
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
            "ProfileTransferLegacyWarning",
            "ProfileTransferCodeTitle",
            "ProfileTransferCodeInstructions",
            "ProfileTransferCodePlaceholder",
            "ProfileTransferCodeInvalid",
            "ProfileTransferCodeMessage"
        };

        service.Initialize(CultureInfo.GetCultureInfo("en-US"));
        foreach (var language in new[] { "en", "es", "de" })
        {
            service.SetLanguage(language);
            Assert.All(keys, key => Assert.NotEqual(key, service.Get(key)));
        }
    }

    [Fact]
    public void Help_transfer_copy_is_declared_for_every_supported_language()
    {
        var keys = new[]
        {
            "HelpTransferTitle",
            "HelpTransferBody",
            "HelpTransferCodeBody",
            "HelpTransferUsefulness",
            "HelpTransferSafety",
            "Previous",
            "Next"
        };
        var resourceDirectory = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Anthropometry.App", "Resources", "Strings"));

        foreach (var language in LanguageService.SupportedLanguages)
        {
            var resourceName = language.Code == "en"
                ? "AppResources.resx"
                : $"AppResources.{language.Code}.resx";
            var markup = File.ReadAllText(Path.Combine(resourceDirectory, resourceName));

            Assert.All(keys, key => Assert.Contains($"name=\"{key}\"", markup));
        }
    }

    [Fact]
    public void Help_reminder_copy_is_declared_for_every_supported_language()
    {
        var keys = new[]
        {
            "HelpRemindersTitle",
            "HelpDailyReminderBody",
            "HelpInactivityReminderBody",
            "HelpReminderPrivacy"
        };
        var resourceDirectory = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Anthropometry.App", "Resources", "Strings"));

        foreach (var language in LanguageService.SupportedLanguages)
        {
            var resourceName = language.Code == "en"
                ? "AppResources.resx"
                : $"AppResources.{language.Code}.resx";
            var markup = File.ReadAllText(Path.Combine(resourceDirectory, resourceName));

            Assert.All(keys, key => Assert.Contains($"name=\"{key}\"", markup));
        }
    }

    [Fact]
    public void Device_language_is_used_only_until_the_user_saves_an_override()
    {
        var preferences = new FakeLanguagePreferenceStore();
        var service = new LanguageService(preferences);

        service.Initialize(CultureInfo.GetCultureInfo("fr-FR"));

        Assert.Equal("fr", service.CurrentLanguageCode);
        Assert.Null(preferences.LanguageCode);

        service.SetLanguage("de");
        service.Initialize(CultureInfo.GetCultureInfo("fr-FR"));

        Assert.Equal("de", service.CurrentLanguageCode);
        Assert.Equal("de", preferences.LanguageCode);
    }

    [Fact]
    public void Device_language_matches_supported_language_by_language_code()
    {
        var service = new LanguageService(new FakeLanguagePreferenceStore());

        service.Initialize(CultureInfo.GetCultureInfo("pt-PT"));

        Assert.Equal("pt-BR", service.CurrentLanguageCode);
    }

    [Fact]
    public void Unsupported_device_language_falls_back_to_english()
    {
        var service = new LanguageService(new FakeLanguagePreferenceStore());

        service.Initialize(CultureInfo.GetCultureInfo("pl-PL"));

        Assert.Equal("en", service.CurrentLanguageCode);
    }

    [Fact]
    public void Arabic_language_option_is_marked_right_to_left()
    {
        var arabic = LanguageService.SupportedLanguages.Single(language => language.Code == "ar");
        var french = LanguageService.SupportedLanguages.Single(language => language.Code == "fr");

        Assert.True(arabic.IsRightToLeft);
        Assert.False(french.IsRightToLeft);
    }

    [Fact]
    public void Core_localized_copy_is_available_for_supported_languages()
    {
        var service = new LanguageService(new FakeLanguagePreferenceStore());
        var requiredKeys = new[] { "SettingsTitle", "Language", "AddProfile", "Save", "Cancel" };

        service.Initialize(CultureInfo.GetCultureInfo("en-US"));
        foreach (var language in LanguageService.SupportedLanguages.Where(language => language.Code != "en"))
        {
            service.SetLanguage(language.Code);
            Assert.All(requiredKeys, key => Assert.NotEqual(key, service.Get(key)));
        }
    }

    private sealed class FakeLanguagePreferenceStore : ILanguagePreferenceStore
    {
        public string? LanguageCode { get; set; }

        public string? GetLanguageCode() => LanguageCode;

        public void SetLanguageCode(string code) => LanguageCode = code;
    }
}
