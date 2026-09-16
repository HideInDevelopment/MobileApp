using Microsoft.Maui.Storage;

namespace Anthropometry.App.Display;

public sealed class PreferencesDisplayPreferenceStore : IDisplayPreferenceStore
{
    private const string DateFormatKey = "date-format";
    private const string WeightUnitKey = "weight-unit";
    private const string HeightUnitKey = "height-unit";
    private const string MeasurementSystemKey = "measurement-system";

    public string? GetDateFormatCode() => Get(DateFormatKey);

    public string? GetWeightUnitCode() => Get(WeightUnitKey);

    public string? GetHeightUnitCode() => Get(HeightUnitKey);

    public string? GetMeasurementSystemCode() => Get(MeasurementSystemKey);

    public void SetDateFormatCode(string code) => Preferences.Default.Set(DateFormatKey, code);

    public void SetWeightUnitCode(string code) => Preferences.Default.Set(WeightUnitKey, code);

    public void SetHeightUnitCode(string code) => Preferences.Default.Set(HeightUnitKey, code);

    public void SetMeasurementSystemCode(string code) => Preferences.Default.Set(MeasurementSystemKey, code);

    private static string? Get(string key)
    {
        var value = Preferences.Default.Get(key, string.Empty);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
