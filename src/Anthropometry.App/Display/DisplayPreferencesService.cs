using System.Globalization;

namespace Anthropometry.App.Display;

public sealed class DisplayPreferencesService
{
    public const string MetricCode = "metric";
    public const string ImperialCode = "imperial";
    public const string DayMonthYearCode = "dd/MM/yyyy";
    public const string MonthDayYearCode = "MM/dd/yyyy";
    public const string KilogramsCode = "kg";
    public const string PoundsCode = "lb";
    public const string MetersCode = "m";
    public const string CentimetersCode = "cm";
    public const string FeetCode = "ft";
    public const string InchesCode = "in";

    private const decimal PoundsPerKilogram = 2.2046226218m;
    private const decimal CentimetersPerMeter = 100m;
    private const decimal CentimetersPerFoot = 30.48m;
    private const decimal CentimetersPerInch = 2.54m;

    private readonly IDisplayPreferenceStore _preferences;

    public DisplayPreferencesService(IDisplayPreferenceStore preferences)
    {
        _preferences = preferences;
    }

    public event EventHandler? PreferencesChanged;

    public string MeasurementSystemCode { get; private set; } = MetricCode;

    public string DateFormatCode { get; private set; } = DayMonthYearCode;

    public string WeightUnitCode => MeasurementSystemCode == ImperialCode ? PoundsCode : KilogramsCode;

    public string HeightUnitCode => MeasurementSystemCode == ImperialCode ? FeetCode : MetersCode;

    public string CircumferenceUnitCode => MeasurementSystemCode == ImperialCode ? InchesCode : CentimetersCode;

    public void Initialize()
    {
        DateFormatCode = IsDateFormatSupported(_preferences.GetDateFormatCode())
            ? _preferences.GetDateFormatCode()!
            : DayMonthYearCode;
        var savedMeasurementSystemCode = _preferences.GetMeasurementSystemCode();
        MeasurementSystemCode = IsMeasurementSystemSupported(savedMeasurementSystemCode)
            ? savedMeasurementSystemCode!
            : InferLegacyMeasurementSystem();
        if (!string.Equals(savedMeasurementSystemCode, MeasurementSystemCode, StringComparison.Ordinal))
        {
            _preferences.SetMeasurementSystemCode(MeasurementSystemCode);
        }
    }

    public void SetDateFormat(string code)
    {
        if (!IsDateFormatSupported(code))
        {
            throw new ArgumentException($"Unsupported date format code: {code}", nameof(code));
        }

        if (string.Equals(DateFormatCode, code, StringComparison.Ordinal))
        {
            return;
        }

        DateFormatCode = code;
        _preferences.SetDateFormatCode(code);
        PreferencesChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetMeasurementSystem(string code)
    {
        if (!IsMeasurementSystemSupported(code))
        {
            throw new ArgumentException($"Unsupported measurement system code: {code}", nameof(code));
        }

        if (string.Equals(MeasurementSystemCode, code, StringComparison.Ordinal))
        {
            return;
        }

        MeasurementSystemCode = code;
        _preferences.SetMeasurementSystemCode(code);
        PreferencesChanged?.Invoke(this, EventArgs.Empty);
    }

    public decimal ToDisplayWeight(decimal weightKg)
        => ConvertWeightToDisplay(weightKg, WeightUnitCode);

    public decimal ToMetricWeight(decimal weight)
        => ConvertWeightToMetric(weight, WeightUnitCode);

    public decimal ToDisplayHeight(decimal heightCm)
        => ConvertHeightToDisplay(heightCm, HeightUnitCode);

    public decimal ToMetricHeight(decimal height)
        => ConvertHeightToMetric(height, HeightUnitCode);

    public decimal ToDisplayCircumference(decimal circumferenceCm)
        => ConvertCircumferenceToDisplay(circumferenceCm, CircumferenceUnitCode);

    public decimal ToMetricCircumference(decimal circumference)
        => ConvertCircumferenceToMetric(circumference, CircumferenceUnitCode);

    public static decimal ConvertWeightToDisplay(decimal weightKg, string unitCode)
        => unitCode == PoundsCode ? weightKg * PoundsPerKilogram : weightKg;

    public static decimal ConvertWeightToMetric(decimal weight, string unitCode)
        => unitCode == PoundsCode ? weight / PoundsPerKilogram : weight;

    public static decimal ConvertHeightToDisplay(decimal heightCm, string unitCode)
        => unitCode switch
        {
            MetersCode => heightCm / CentimetersPerMeter,
            FeetCode => heightCm / CentimetersPerFoot,
            _ => heightCm
        };

    public static decimal ConvertHeightToMetric(decimal height, string unitCode)
        => unitCode switch
        {
            MetersCode => height * CentimetersPerMeter,
            FeetCode => height * CentimetersPerFoot,
            _ => height
        };

    public static decimal ConvertCircumferenceToDisplay(decimal circumferenceCm, string unitCode)
        => unitCode == InchesCode ? circumferenceCm / CentimetersPerInch : circumferenceCm;

    public static decimal ConvertCircumferenceToMetric(decimal circumference, string unitCode)
        => unitCode == InchesCode ? circumference * CentimetersPerInch : circumference;

    public string FormatDate(DateTimeOffset measuredAtUtc)
        => measuredAtUtc.ToLocalTime().ToString(DateFormatCode, CultureInfo.InvariantCulture);

    public string FormatCompactDate(DateTimeOffset measuredAtUtc)
        => measuredAtUtc.ToLocalTime().ToString(
            DateFormatCode == MonthDayYearCode ? "MM/dd" : "dd/MM",
            CultureInfo.InvariantCulture);

    private static bool IsDateFormatSupported(string? code)
        => code is DayMonthYearCode or MonthDayYearCode;

    private string InferLegacyMeasurementSystem()
    {
        var legacyHeightUnitCode = _preferences.GetHeightUnitCode();
        var legacyWeightUnitCode = _preferences.GetWeightUnitCode();
        return legacyHeightUnitCode is FeetCode or InchesCode || legacyWeightUnitCode == PoundsCode
            ? ImperialCode
            : MetricCode;
    }

    private static bool IsMeasurementSystemSupported(string? code)
        => code is MetricCode or ImperialCode;
}
