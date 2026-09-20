using System.Globalization;
using System.Resources;

namespace Anthropometry.App.Localization;

public sealed record LanguageOption(string Code, string DisplayNameKey, string CultureName)
{
    public CultureInfo Culture => CultureInfo.GetCultureInfo(CultureName);

    public bool IsRightToLeft => Culture.TextInfo.IsRightToLeft;
}

public sealed class LanguageService
{
    private static readonly IReadOnlyList<LanguageOption> Languages =
    [
        new("en", "LanguageEnglish", "en-US"),
        new("es", "LanguageSpanish", "es-ES"),
        new("de", "LanguageGerman", "de-DE"),
        new("fr", "LanguageFrench", "fr-FR"),
        new("pt-BR", "LanguagePortugueseBrazil", "pt-BR"),
        new("it", "LanguageItalian", "it-IT"),
        new("ja", "LanguageJapanese", "ja-JP"),
        new("ko", "LanguageKorean", "ko-KR"),
        new("zh-CN", "LanguageSimplifiedChinese", "zh-CN"),
        new("ar", "LanguageArabic", "ar-SA"),
        new("hi", "LanguageHindi", "hi-IN")
    ];

    public static IReadOnlyList<LanguageOption> SupportedLanguages => Languages;

    public static IReadOnlyList<string> ResourceKeys { get; } =
    [
        "AppTitle", "ProfilesTitle", "SettingsTitle", "HelpTitle", "Retry",
        "DatabaseUnavailableTitle", "DatabaseUnavailableMessage",
        "SettingsLabel", "HelpLabel", "AddProfile", "LoadingProfiles", "NoProfileYet",
        "CreateOneToGetStarted", "CreateProfile", "OpenProfile", "DeleteProfile",
        "ProfileName", "Height", "Age", "Gender", "Male", "Female", "GenderRequired", "ActivityLevel", "Years", "Save", "Cancel",
        "CreateProfileTitle", "EditProfileTitle", "ProfileNameRequired", "ValidProfileSettings",
        "ProfileLimitReached", "SaveProfileError", "LoadProfilesError", "DeleteProfileError",
        "ProfileDetailsError", "AddWeight", "AddMeasurements", "EditProfile", "History", "ExportProfile", "ImportProfile",
        "ExportProfileTitle", "ImportProfileTitle", "ProfileTransferPrivacyWarning", "ProfileTransferConfirmation",
        "ProfileTransferSuccess", "ProfileTransferExportError", "ProfileTransferImportError", "ProfileTransferInvalidFile",
        "ProfileTransferUnsupportedFormat", "ProfileTransferLimitReached", "ProfileTransferPassphraseTitle",
        "ProfileTransferPassphraseInstructions", "ProfileTransferPassphrasePlaceholder",
        "ProfileTransferPassphraseConfirmationPlaceholder", "ProfileTransferPassphraseInvalid",
        "ProfileTransferPassphraseMismatch", "ProfileTransferPasswordRequired",
        "ProfileTransferAuthenticationFailed", "ProfileTransferLegacyWarning",
        "ProfileTransferCodeTitle", "ProfileTransferCodeInstructions", "ProfileTransferCodePlaceholder",
        "ProfileTransferCodeInvalid", "ProfileTransferCodeMessage",
        "EstimateDisclaimer", "ResultsEstimateDisclaimer", "Weight", "Neck", "Abdomen", "Waist", "Hip",
        "Kg", "Cm", "SaveWeight", "CalculateResults", "ValidMeasurementExtended", "ValidWeight",
        "ValidMeasurementExtendedFemale",
        "MeasurementValuesError", "CompleteProfileDetails", "SaveMeasurementError", "CalculationError",
        "MeasurementHistory", "NoMeasurements", "SavedMeasurementsLocal", "ViewResults", "ViewHistory", "EditMeasurement", "EditMeasurementTitle", "DeleteMeasurement", "DeleteMeasurementTitle", "DeleteMeasurementMessage", "DeleteMeasurementError", "WeightOnly", "LoadHistoryError",
        "Filter", "ClearFilters", "FromDate", "ToDate", "MeasurementType", "AllMeasurements", "NoMatchingMeasurements", "NoMatchingMeasurementsDescription", "InvalidHistoryDateRange",
        "WeightAndSizes", "ReusedMeasurementsDescription", "EstimatedResults", "ResultsTitle",
        "BodyFatPercentage", "BasalMetabolicRate", "TotalDailyEnergyExpenditure", "ResultsLoadError",
        "Language", "LanguageEnglish", "LanguageSpanish", "LanguageGerman", "LanguageFrench", "LanguagePortugueseBrazil",
        "LanguageItalian", "LanguageJapanese", "LanguageKorean", "LanguageSimplifiedChinese", "LanguageArabic", "LanguageHindi",
        "Sedentary", "LightlyActive", "ModeratelyActive", "HighlyActive", "VeryHighlyActive",
        "DeleteProfileTitle", "DeleteProfileMessage", "DeleteAction",
        "Charts", "MetricChart", "MetricChartTitle", "MetricChartNoMeasurements", "MetricChartLoadError", "InvalidMetricChartDateRange", "ChartMetric", "WeightGraphic", "WeightGraphicTitle", "WeightGraphicNoMeasurements", "WeightGraphicLoadError", "Date",
        "HelpAboutTitle", "HelpDisclaimer", "HelpMeasurementConsistency", "HelpEquationsTitle",
        "HelpBodyFatTitle", "HelpBodyFatDescription", "HelpBodyFatFormula", "HelpFemaleBodyFatTitle",
        "HelpFemaleBodyFatDescription", "HelpFemaleBodyFatFormula", "HelpBmrTitle", "HelpBmrDescription",
        "HelpBmrFormula", "HelpFemaleBmrTitle", "HelpFemaleBmrDescription", "HelpFemaleBmrFormula",
        "HelpTdeeTitle", "HelpTdeeDescription", "HelpTdeeFormula",
        "HelpActivityFactors", "HelpUnits", "HelpTransferTitle", "HelpTransferBody",
        "HelpTransferCodeBody", "HelpTransferUsefulness", "HelpTransferSafety", "Close", "Theme", "LightTheme", "DarkTheme",
        "Previous", "Next",
        "HelpRemindersTitle", "HelpDailyReminderBody", "HelpInactivityReminderBody", "HelpReminderPrivacy",
        "GuidanceActivityLevelTitle", "GuidanceActivityLevelBody", "GuidanceWeightTitle", "GuidanceWeightBody",
        "GuidanceNeckTitle", "GuidanceNeckBody", "GuidanceAbdomenTitle", "GuidanceAbdomenBody",
        "GuidanceWaistTitle", "GuidanceWaistBody", "GuidanceHipTitle", "GuidanceHipBody",
        "GuidanceBodyFatTitle", "GuidanceBodyFatBody", "GuidanceBmrTitle", "GuidanceBmrBody",
        "GuidanceTdeeTitle", "GuidanceTdeeBody", "GuidanceReusedSizesTitle", "GuidanceReusedSizesBody",
        "GuidanceEstimateDisclaimerTitle", "GuidanceEstimateDisclaimerBody",
        "ActivityLevelSedentaryDescription", "ActivityLevelLightDescription",
        "ActivityLevelModerateDescription", "ActivityLevelHighDescription", "ActivityLevelVeryHighDescription",
        "DateFormat", "DayMonthYear", "MonthDayYear", "WeightUnit", "HeightUnit",
        "Kilograms", "Pounds", "Centimeters", "Feet", "Meters", "Inches",
        "MeasurementSystem", "Metric", "Imperial", "Lb", "Kg", "Cm", "M", "Ft", "In",
        "ReminderSettings", "DailyReminder", "ReminderTime", "InactivityReminder", "InactivityReminderInterval",
        "SevenDays", "FourteenDays", "ReminderPermissionRequired"
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

    public void Initialize(CultureInfo? deviceCulture = null)
    {
        var savedCode = _preferences.GetLanguageCode();
        var language = Languages.FirstOrDefault(candidate =>
            string.Equals(candidate.Code, savedCode, StringComparison.OrdinalIgnoreCase))
            ?? FindByCulture(deviceCulture ?? CultureInfo.InstalledUICulture)
            ?? Languages[0];
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

    private static LanguageOption? FindByCulture(CultureInfo culture)
        => Languages.FirstOrDefault(candidate =>
            string.Equals(candidate.Culture.Name, culture.Name, StringComparison.OrdinalIgnoreCase))
            ?? Languages.FirstOrDefault(candidate =>
                string.Equals(
                    candidate.Culture.TwoLetterISOLanguageName,
                    culture.TwoLetterISOLanguageName,
                    StringComparison.OrdinalIgnoreCase));
}
