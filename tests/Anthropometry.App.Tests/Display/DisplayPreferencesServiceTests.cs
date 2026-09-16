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

        Assert.Equal(DisplayPreferencesService.MetricCode, service.MeasurementSystemCode);
        Assert.Equal(DisplayPreferencesService.DayMonthYearCode, service.DateFormatCode);
        Assert.Equal(DisplayPreferencesService.KilogramsCode, service.WeightUnitCode);
        Assert.Equal(DisplayPreferencesService.MetersCode, service.HeightUnitCode);
        Assert.Equal(DisplayPreferencesService.CentimetersCode, service.CircumferenceUnitCode);
    }

    [Fact]
    public void Initialize_falls_back_to_defaults_for_unknown_saved_codes()
    {
        var store = new InMemoryDisplayPreferenceStore
        {
            DateFormatCode = "unknown-date",
            WeightUnitCode = "stone",
            MeasurementSystemCode = "foot"
        };
        var service = new DisplayPreferencesService(store);

        service.Initialize();

        Assert.Equal(DisplayPreferencesService.DayMonthYearCode, service.DateFormatCode);
        Assert.Equal(DisplayPreferencesService.KilogramsCode, service.WeightUnitCode);
        Assert.Equal(DisplayPreferencesService.MetricCode, service.MeasurementSystemCode);
        Assert.Equal(DisplayPreferencesService.MetersCode, service.HeightUnitCode);
    }

    [Fact]
    public void Initialize_migrates_the_previous_independent_preferences_to_one_system()
    {
        var store = new InMemoryDisplayPreferenceStore
        {
            HeightUnitCode = "in",
            WeightUnitCode = "kg"
        };
        var service = new DisplayPreferencesService(store);

        service.Initialize();

        Assert.Equal(DisplayPreferencesService.ImperialCode, service.MeasurementSystemCode);
        Assert.Equal(DisplayPreferencesService.ImperialCode, store.MeasurementSystemCode);
        Assert.Equal(DisplayPreferencesService.FeetCode, service.HeightUnitCode);
        Assert.Equal(DisplayPreferencesService.InchesCode, service.CircumferenceUnitCode);
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
        service.SetMeasurementSystem(DisplayPreferencesService.ImperialCode);

        Assert.Equal(DisplayPreferencesService.MonthDayYearCode, store.DateFormatCode);
        Assert.Equal(DisplayPreferencesService.PoundsCode, service.WeightUnitCode);
        Assert.Equal(DisplayPreferencesService.ImperialCode, store.MeasurementSystemCode);
        Assert.Equal(2, changes);
    }

    [Fact]
    public void Conversion_round_trips_weight_and_height_at_the_presentation_boundary()
    {
        var service = new DisplayPreferencesService(new InMemoryDisplayPreferenceStore());
        service.Initialize();
        service.SetMeasurementSystem(DisplayPreferencesService.ImperialCode);

        var pounds = service.ToDisplayWeight(80m);
        var feet = service.ToDisplayHeight(180m);
        var inches = service.ToDisplayCircumference(40m);

        Assert.Equal(176.369809744m, pounds, 9);
        Assert.Equal(5.905511811m, feet, 9);
        Assert.Equal(15.748031496m, inches, 9);
        Assert.Equal(80m, service.ToMetricWeight(pounds), 9);
        Assert.Equal(180m, service.ToMetricHeight(feet), 9);
        Assert.Equal(40m, service.ToMetricCircumference(inches), 9);
    }

    [Fact]
    public void Metric_height_is_displayed_in_meters_and_circumference_in_centimeters()
    {
        var service = new DisplayPreferencesService(new InMemoryDisplayPreferenceStore());
        service.Initialize();

        Assert.Equal(1.8m, service.ToDisplayHeight(180m));
        Assert.Equal(40m, service.ToDisplayCircumference(40m));
        Assert.Equal(180m, service.ToMetricHeight(1.8m));
        Assert.Equal(40m, service.ToMetricCircumference(40m));
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

        public string? MeasurementSystemCode { get; set; }

        public string? GetDateFormatCode() => DateFormatCode;

        public string? GetWeightUnitCode() => WeightUnitCode;

        public string? GetHeightUnitCode() => HeightUnitCode;

        public string? GetMeasurementSystemCode() => MeasurementSystemCode;

        public void SetDateFormatCode(string code) => DateFormatCode = code;

        public void SetWeightUnitCode(string code) => WeightUnitCode = code;

        public void SetHeightUnitCode(string code) => HeightUnitCode = code;

        public void SetMeasurementSystemCode(string code) => MeasurementSystemCode = code;
    }
}
