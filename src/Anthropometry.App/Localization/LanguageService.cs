using System.Globalization;
using System.Resources;

namespace Anthropometry.App.Localization;

public sealed record LanguageOption(string Code, string DisplayName, string CultureName)
{
    public CultureInfo Culture => CultureInfo.GetCultureInfo(CultureName);
}

public sealed class LanguageService
{
    private static readonly IReadOnlyList<LanguageOption> Languages =
    [
        new("en", "English", "en-US"),
        new("es", "Español", "es-ES"),
        new("de", "Deutsch", "de-DE")
    ];

    public static IReadOnlyList<LanguageOption> SupportedLanguages => Languages;

    public static IReadOnlyList<string> ResourceKeys { get; } =
    [
        "AppTitle", "ProfilesTitle", "SettingsTitle", "HelpTitle", "Retry",
        "DatabaseUnavailableTitle", "DatabaseUnavailableMessage",
        "SettingsLabel", "HelpLabel", "AddProfile", "LoadingProfiles", "NoProfileYet",
        "CreateOneToGetStarted", "CreateProfile", "OpenProfile", "DeleteProfile",
        "ProfileName", "Height", "Age", "ActivityLevel", "Years", "Save", "Cancel",
        "CreateProfileTitle", "EditProfileTitle", "ProfileNameRequired", "ValidProfileSettings",
        "ProfileLimitReached", "SaveProfileError", "LoadProfilesError", "DeleteProfileError",
        "ProfileDetailsError", "AddWeight", "AddMeasurements", "EditProfile", "History",
        "EstimateDisclaimer", "ResultsEstimateDisclaimer", "Weight", "Neck", "Abdomen",
        "Kg", "Cm", "SaveWeight", "CalculateResults", "ValidMeasurementExtended", "ValidWeight",
        "MeasurementValuesError", "CompleteProfileDetails", "SaveMeasurementError", "CalculationError",
        "MeasurementHistory", "NoMeasurements", "SavedMeasurementsLocal", "ViewResults", "WeightOnly", "LoadHistoryError",
        "WeightAndSizes", "ReusedMeasurementsDescription", "EstimatedResults", "ResultsTitle",
        "BodyFatPercentage", "BasalMetabolicRate", "TotalDailyEnergyExpenditure", "ResultsLoadError",
        "Language", "Sedentary", "LightlyActive", "ModeratelyActive", "HighlyActive", "VeryHighlyActive",
        "DeleteProfileTitle", "DeleteProfileMessage", "DeleteAction"
    ];

    private readonly ILanguagePreferenceStore _preferences;
    private readonly ResourceManager _resources = new(
        "Anthropometry.App.Resources.Strings.AppResources",
        typeof(LanguageService).Assembly);
    private CultureInfo _culture = CultureInfo.GetCultureInfo("en-US");

    public LanguageService(ILanguagePreferenceStore preferences)
    {
        _preferences = preferences;
    }

    public event EventHandler? LanguageChanged;

    public string CurrentLanguageCode { get; private set; } = "en";

    public void Initialize()
    {
        var savedCode = _preferences.GetLanguageCode();
        var language = Languages.FirstOrDefault(candidate =>
            string.Equals(candidate.Code, savedCode, StringComparison.OrdinalIgnoreCase)) ?? Languages[0];
        Apply(language, persist: false);
    }

    public void SetLanguage(string code)
    {
        var language = Languages.FirstOrDefault(candidate =>
            string.Equals(candidate.Code, code, StringComparison.OrdinalIgnoreCase));
        if (language is null)
        {
            throw new ArgumentException($"Unsupported language code: {code}", nameof(code));
        }

        Apply(language, persist: true);
    }

    public string Get(string key)
        => _resources.GetString(key, _culture)
            ?? _resources.GetString(key, Languages[0].Culture)
            ?? key;

    private void Apply(LanguageOption language, bool persist)
    {
        CurrentLanguageCode = language.Code;
        _culture = language.Culture;
        if (persist)
        {
            _preferences.SetLanguageCode(language.Code);
        }

        CultureInfo.DefaultThreadCurrentCulture = _culture;
        CultureInfo.DefaultThreadCurrentUICulture = _culture;
        CultureInfo.CurrentCulture = _culture;
        CultureInfo.CurrentUICulture = _culture;
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }
}
