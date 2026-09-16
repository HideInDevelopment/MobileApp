using System.Globalization;

namespace Anthropometry.App.Display;

public sealed class DisplayPreferencesService
{
    public const string DayMonthYearCode = "dd/MM/yyyy";
    public const string MonthDayYearCode = "MM/dd/yyyy";
    public const string KilogramsCode = "kg";
    public const string PoundsCode = "lb";
    public const string CentimetersCode = "cm";
    public const string FeetCode = "ft";

    private const decimal PoundsPerKilogram = 2.2046226218m;
    private const decimal CentimetersPerFoot = 30.48m;
    private const string LegacyInchesCode = "in";

    private readonly IDisplayPreferenceStore _preferences;

    public DisplayPreferencesService(IDisplayPreferenceStore preferences)
    {
        _preferences = preferences;
    }

    public event EventHandler? PreferencesChanged;

    public string DateFormatCode { get; private set; } = DayMonthYearCode;

    public string WeightUnitCode { get; private set; } = KilogramsCode;

    public string HeightUnitCode { get; private set; } = CentimetersCode;

    public void Initialize()
    {
        DateFormatCode = IsDateFormatSupported(_preferences.GetDateFormatCode())
            ? _preferences.GetDateFormatCode()!
            : DayMonthYearCode;
        WeightUnitCode = IsWeightUnitSupported(_preferences.GetWeightUnitCode())
            ? _preferences.GetWeightUnitCode()!
            : KilogramsCode;
        var savedHeightUnitCode = _preferences.GetHeightUnitCode();
        HeightUnitCode = savedHeightUnitCode == LegacyInchesCode
            ? FeetCode
            : IsHeightUnitSupported(savedHeightUnitCode)
                ? savedHeightUnitCode!
                : CentimetersCode;
        if (savedHeightUnitCode == LegacyInchesCode)
        {
            _preferences.SetHeightUnitCode(FeetCode);
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

    public void SetWeightUnit(string code)
    {
        if (!IsWeightUnitSupported(code))
        {
            throw new ArgumentException($"Unsupported weight unit code: {code}", nameof(code));
        }

        if (string.Equals(WeightUnitCode, code, StringComparison.Ordinal))
        {
            return;
        }

        WeightUnitCode = code;
        _preferences.SetWeightUnitCode(code);
        PreferencesChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetHeightUnit(string code)
    {
        if (!IsHeightUnitSupported(code))
        {
            throw new ArgumentException($"Unsupported height unit code: {code}", nameof(code));
        }

        if (string.Equals(HeightUnitCode, code, StringComparison.Ordinal))
        {
            return;
        }

        HeightUnitCode = code;
        _preferences.SetHeightUnitCode(code);
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

    public static decimal ConvertWeightToDisplay(decimal weightKg, string unitCode)
        => unitCode == PoundsCode ? weightKg * PoundsPerKilogram : weightKg;

    public static decimal ConvertWeightToMetric(decimal weight, string unitCode)
        => unitCode == PoundsCode ? weight / PoundsPerKilogram : weight;

    public static decimal ConvertHeightToDisplay(decimal heightCm, string unitCode)
        => unitCode == FeetCode ? heightCm / CentimetersPerFoot : heightCm;

    public static decimal ConvertHeightToMetric(decimal height, string unitCode)
        => unitCode == FeetCode ? height * CentimetersPerFoot : height;

    public string FormatDate(DateTimeOffset measuredAtUtc)
        => measuredAtUtc.ToLocalTime().ToString(DateFormatCode, CultureInfo.InvariantCulture);

    public string FormatCompactDate(DateTimeOffset measuredAtUtc)
        => measuredAtUtc.ToLocalTime().ToString(
            DateFormatCode == MonthDayYearCode ? "MM/dd" : "dd/MM",
            CultureInfo.InvariantCulture);

    private static bool IsDateFormatSupported(string? code)
        => code is DayMonthYearCode or MonthDayYearCode;

    private static bool IsWeightUnitSupported(string? code)
        => code is KilogramsCode or PoundsCode;

    private static bool IsHeightUnitSupported(string? code)
        => code is CentimetersCode or FeetCode;
}
