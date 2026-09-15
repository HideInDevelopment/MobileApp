using System.Globalization;
using Anthropometry.App.Display;

namespace Anthropometry.App.Tests.Display;

public sealed class DisplayPreferencesServiceTests
{
    [Fact]
    public void Initialize_uses_metric_and_day_month_defaults_when_store_is_empty()
    {
        var store = new InMemoryDisplayPreferenceStore();
        var service = new DisplayPreferencesService(store);

        service.Initialize();

        Assert.Equal(DisplayPreferencesService.DayMonthYearCode, service.DateFormatCode);
        Assert.Equal(DisplayPreferencesService.KilogramsCode, service.WeightUnitCode);
        Assert.Equal(DisplayPreferencesService.CentimetersCode, service.HeightUnitCode);
    }

    [Fact]
    public void Initialize_falls_back_to_defaults_for_unknown_saved_codes()
    {
        var store = new InMemoryDisplayPreferenceStore
        {
            DateFormatCode = "unknown-date",
            WeightUnitCode = "stone",
            HeightUnitCode = "foot"
        };
        var service = new DisplayPreferencesService(store);

        service.Initialize();

        Assert.Equal(DisplayPreferencesService.DayMonthYearCode, service.DateFormatCode);
        Assert.Equal(DisplayPreferencesService.KilogramsCode, service.WeightUnitCode);
        Assert.Equal(DisplayPreferencesService.CentimetersCode, service.HeightUnitCode);
    }

    [Fact]
    public void Set_preferences_persists_values_and_notifies_once_per_changed_value()
    {
        var store = new InMemoryDisplayPreferenceStore();
        var service = new DisplayPreferencesService(store);
        service.Initialize();
        var changes = 0;
        service.PreferencesChanged += (_, _) => changes++;

        service.SetDateFormat(DisplayPreferencesService.MonthDayYearCode);
        service.SetWeightUnit(DisplayPreferencesService.PoundsCode);
        service.SetHeightUnit(DisplayPreferencesService.InchesCode);

        Assert.Equal(DisplayPreferencesService.MonthDayYearCode, store.DateFormatCode);
        Assert.Equal(DisplayPreferencesService.PoundsCode, store.WeightUnitCode);
        Assert.Equal(DisplayPreferencesService.InchesCode, store.HeightUnitCode);
        Assert.Equal(3, changes);
    }

    [Fact]
    public void Conversion_round_trips_weight_and_height_at_the_presentation_boundary()
    {
        var service = new DisplayPreferencesService(new InMemoryDisplayPreferenceStore());
        service.Initialize();
        service.SetWeightUnit(DisplayPreferencesService.PoundsCode);
        service.SetHeightUnit(DisplayPreferencesService.InchesCode);

        var pounds = service.ToDisplayWeight(80m);
        var inches = service.ToDisplayHeight(180m);

        Assert.Equal(176.369809744m, pounds, 9);
        Assert.Equal(70.8661417323m, inches, 9);
        Assert.Equal(80m, service.ToMetricWeight(pounds), 9);
        Assert.Equal(180m, service.ToMetricHeight(inches), 9);
    }

    [Fact]
    public void Date_format_uses_local_date_with_selected_order()
    {
        var service = new DisplayPreferencesService(new InMemoryDisplayPreferenceStore());
        service.Initialize();
        var date = new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);

        Assert.Equal("08/09/2026", service.FormatDate(date));

        service.SetDateFormat(DisplayPreferencesService.MonthDayYearCode);

        Assert.Equal("09/08/2026", service.FormatDate(date));
        Assert.Equal("09/08", service.FormatCompactDate(date));
    }

    [Fact]
    public void Date_format_keeps_the_selected_slash_separator_in_german_culture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var service = new DisplayPreferencesService(new InMemoryDisplayPreferenceStore());
            service.Initialize();

            Assert.Equal("08/09/2026", service.FormatDate(new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero)));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    private sealed class InMemoryDisplayPreferenceStore : IDisplayPreferenceStore
    {
        public string? DateFormatCode { get; set; }

        public string? WeightUnitCode { get; set; }

        public string? HeightUnitCode { get; set; }

        public string? GetDateFormatCode() => DateFormatCode;

        public string? GetWeightUnitCode() => WeightUnitCode;

        public string? GetHeightUnitCode() => HeightUnitCode;

        public void SetDateFormatCode(string code) => DateFormatCode = code;

        public void SetWeightUnitCode(string code) => WeightUnitCode = code;

        public void SetHeightUnitCode(string code) => HeightUnitCode = code;
    }
}
