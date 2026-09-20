using Anthropometry.Application.Entitlements;

namespace Anthropometry.App.Display;

public sealed class EntitledDisplayPreferences
{
    private readonly DisplayPreferencesService _displayPreferences;
    private EntitlementSnapshot _entitlement;

    public EntitledDisplayPreferences(
        DisplayPreferencesService displayPreferences,
        EntitlementSnapshot entitlement)
    {
        _displayPreferences = displayPreferences;
        _entitlement = entitlement;
    }

    public string MeasurementSystemCode
        => FeatureAccessPolicy.CanUse(_entitlement, PremiumFeature.ImperialUnits)
            ? _displayPreferences.MeasurementSystemCode
            : DisplayPreferencesService.MetricCode;

    public string WeightUnitCode
        => MeasurementSystemCode == DisplayPreferencesService.ImperialCode
            ? DisplayPreferencesService.PoundsCode
            : DisplayPreferencesService.KilogramsCode;

    public string HeightUnitCode
        => MeasurementSystemCode == DisplayPreferencesService.ImperialCode
            ? DisplayPreferencesService.FeetCode
            : DisplayPreferencesService.MetersCode;

    public string CircumferenceUnitCode
        => MeasurementSystemCode == DisplayPreferencesService.ImperialCode
            ? DisplayPreferencesService.InchesCode
            : DisplayPreferencesService.CentimetersCode;

    public void SetEntitlement(EntitlementSnapshot entitlement)
        => _entitlement = entitlement;

    public decimal ToDisplayWeight(decimal weightKg)
        => DisplayPreferencesService.ConvertWeightToDisplay(weightKg, WeightUnitCode);

    public decimal ToMetricWeight(decimal weight)
        => DisplayPreferencesService.ConvertWeightToMetric(weight, WeightUnitCode);

    public decimal ToDisplayHeight(decimal heightCm)
        => DisplayPreferencesService.ConvertHeightToDisplay(heightCm, HeightUnitCode);

    public decimal ToMetricHeight(decimal height)
        => DisplayPreferencesService.ConvertHeightToMetric(height, HeightUnitCode);

    public decimal ToDisplayCircumference(decimal circumferenceCm)
        => DisplayPreferencesService.ConvertCircumferenceToDisplay(circumferenceCm, CircumferenceUnitCode);

    public decimal ToMetricCircumference(decimal circumference)
        => DisplayPreferencesService.ConvertCircumferenceToMetric(circumference, CircumferenceUnitCode);

    public string FormatDate(DateTimeOffset measuredAtUtc)
        => _displayPreferences.FormatDate(measuredAtUtc);

    public string FormatCompactDate(DateTimeOffset measuredAtUtc)
        => _displayPreferences.FormatCompactDate(measuredAtUtc);
}
