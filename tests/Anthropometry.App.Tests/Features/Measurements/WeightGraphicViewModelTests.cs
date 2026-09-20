using Anthropometry.App.Features.Measurements;
using Anthropometry.App.Display;
using Anthropometry.App.Tests.Support;
using Anthropometry.Application.Abstractions;
using Anthropometry.Application.Calculations;
using Anthropometry.Application.Measurements;
using Anthropometry.Application.Entitlements;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Measurements;
using Xunit;

namespace Anthropometry.App.Tests.Features.Measurements;

public sealed class WeightGraphicViewModelTests
{
    [Fact]
    public async Task Load_maps_all_measurements_to_oldest_first_weight_points()
    {
        var profile = TestData.Profile();
        var repository = new FakeMeasurementRepository();
        repository.Items.Add(CreateMeasurement(profile.Id, 82m, new DateTimeOffset(2026, 9, 8, 8, 0, 0, TimeSpan.Zero)));
        repository.Items.Add(CreateMeasurement(profile.Id, 80m, new DateTimeOffset(2026, 9, 6, 8, 0, 0, TimeSpan.Zero)));
        repository.Items.Add(CreateMeasurement(profile.Id, 81m, new DateTimeOffset(2026, 9, 7, 8, 0, 0, TimeSpan.Zero)));
        var viewModel = CreateViewModel(repository, profile.Id);

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(3, viewModel.Points.Count);
        Assert.Equal(80m, viewModel.Points[0].WeightKg);
        Assert.Equal("06/09", viewModel.Points[0].DateText);
        Assert.Equal(81m, viewModel.Points[1].WeightKg);
        Assert.Equal("07/09", viewModel.Points[1].DateText);
        Assert.Equal(82m, viewModel.Points[2].WeightKg);
        Assert.Equal("08/09", viewModel.Points[2].DateText);
        Assert.False(viewModel.IsEmpty);
    }

    [Fact]
    public async Task Load_adds_ten_kg_to_both_sides_of_the_weight_range()
    {
        var profile = TestData.Profile();
        var repository = new FakeMeasurementRepository();
        repository.Items.Add(CreateMeasurement(profile.Id, 82m, new DateTimeOffset(2026, 9, 8, 8, 0, 0, TimeSpan.Zero)));
        repository.Items.Add(CreateMeasurement(profile.Id, 80m, new DateTimeOffset(2026, 9, 6, 8, 0, 0, TimeSpan.Zero)));
        var viewModel = CreateViewModel(repository, profile.Id);

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(70m, viewModel.ChartMinimumWeight);
        Assert.Equal(92m, viewModel.ChartMaximumWeight);
    }

    [Fact]
    public async Task Chart_width_uses_compact_spacing_between_points()
    {
        var profile = TestData.Profile();
        var repository = new FakeMeasurementRepository();
        foreach (var day in Enumerable.Range(0, 10))
        {
            var measuredAtUtc = new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero).AddDays(day);
            repository.Items.Add(CreateMeasurement(profile.Id, 80m + day, measuredAtUtc));
        }
        var viewModel = CreateViewModel(repository, profile.Id);

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(472d, viewModel.ChartWidth);
    }

    [Fact]
    public async Task Selecting_a_point_shows_its_date_and_weight_legend()
    {
        var profile = TestData.Profile();
        var repository = new FakeMeasurementRepository();
        repository.Items.Add(CreateMeasurement(profile.Id, 80.5m, new DateTimeOffset(2026, 9, 6, 8, 0, 0, TimeSpan.Zero)));
        var viewModel = CreateViewModel(repository, profile.Id);

        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.SelectPoint(viewModel.Points[0]);

        Assert.True(viewModel.IsLegendVisible);
        Assert.Contains("06/09", viewModel.LegendText);
        Assert.Contains("80.5", viewModel.LegendText);
    }

    [Fact]
    public async Task Load_uses_selected_units_for_chart_values_and_legend()
    {
        var profile = TestData.Profile();
        var repository = new FakeMeasurementRepository();
        repository.Items.Add(CreateMeasurement(profile.Id, 80m, new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero)));
        var displayPreferences = TestData.DisplayPreferences();
        displayPreferences.SetDateFormat(DisplayPreferencesService.MonthDayYearCode);
        displayPreferences.SetMeasurementSystem(DisplayPreferencesService.ImperialCode);
        var viewModel = CreateViewModel(repository, profile.Id, displayPreferences: displayPreferences, entitlement: EntitlementTestData.Premium);

        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.SelectPoint(viewModel.Points[0]);

        Assert.Equal("09/08", viewModel.Points[0].DateText);
        Assert.Equal(80m, viewModel.Points[0].WeightKg);
        Assert.Equal(176.369809744m, viewModel.Points[0].DisplayedWeight, 9);
        Assert.Equal(154.323583526m, viewModel.ChartMinimumWeight, 9);
        Assert.Equal(198.416035962m, viewModel.ChartMaximumWeight, 9);
        Assert.Contains("176.37 lb", viewModel.LegendText);
    }

    [Fact]
    public async Task Free_chart_exposes_only_the_kilogram_weight_series()
    {
        var profile = TestData.Profile();
        var repository = new FakeMeasurementRepository();
        repository.Items.Add(CreateMeasurement(profile.Id, 80m, new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero)));
        var displayPreferences = TestData.DisplayPreferences();
        displayPreferences.SetMeasurementSystem(DisplayPreferencesService.ImperialCode);
        var viewModel = CreateViewModel(
            repository,
            profile.Id,
            displayPreferences: displayPreferences,
            entitlement: EntitlementTestData.Free);

        await viewModel.LoadCommand.ExecuteAsync(null);

        var point = Assert.Single(viewModel.Points);
        Assert.Single(viewModel.MetricOptions);
        Assert.Equal(MetricKind.Weight, viewModel.MetricOptions[0].Value);
        Assert.Equal(80m, point.DisplayedWeight);
        Assert.Equal("kg", point.Unit);
        Assert.True(viewModel.IsFullGraphicsLocked);
    }

    [Fact]
    public async Task Selecting_no_point_hides_the_legend()
    {
        var profile = TestData.Profile();
        var repository = new FakeMeasurementRepository();
        repository.Items.Add(CreateMeasurement(profile.Id, 80m, new DateTimeOffset(2026, 9, 6, 8, 0, 0, TimeSpan.Zero)));
        var viewModel = CreateViewModel(repository, profile.Id);

        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.SelectPoint(viewModel.Points[0]);
        viewModel.SelectPoint(null);

        Assert.False(viewModel.IsLegendVisible);
        Assert.Equal(string.Empty, viewModel.LegendText);
    }

    [Fact]
    public void Metric_selector_command_updates_the_selected_metric()
    {
        var viewModel = CreateViewModel(new FakeMeasurementRepository(), TestData.Profile().Id, entitlement: EntitlementTestData.Premium);

        viewModel.SelectMetricCommand.Execute(MetricKind.BodyFatPercentage.ToString());

        Assert.Equal(MetricKind.BodyFatPercentage, viewModel.SelectedMetric);
    }

    [Fact]
    public async Task Selecting_body_fat_metric_uses_persisted_results_and_excludes_missing_results()
    {
        var profile = TestData.Profile();
        var repository = new FakeMeasurementRepository();
        var resultRepository = new FakeCalculationResultRepository();
        var older = CreateMeasurement(profile.Id, 80m, new DateTimeOffset(2026, 9, 6, 8, 0, 0, TimeSpan.Zero));
        var newerWithoutResult = CreateMeasurement(profile.Id, 82m, new DateTimeOffset(2026, 9, 8, 8, 0, 0, TimeSpan.Zero));
        repository.Items.Add(older);
        repository.Items.Add(newerWithoutResult);
        resultRepository.Items.Add(CreateResult(older, CalculationType.BodyFatPercentage, 18.5m, "%"));
        var viewModel = CreateViewModel(repository, profile.Id, resultRepository: resultRepository, entitlement: EntitlementTestData.Premium);

        viewModel.SelectedMetric = MetricKind.BodyFatPercentage;
        await viewModel.LoadCommand.ExecuteAsync(null);

        var point = Assert.Single(viewModel.Points);
        Assert.Equal(older.Id, point.MeasurementId);
        Assert.Equal(18.5m, point.MetricValue);
        Assert.Equal("%", point.MetricUnit);
        viewModel.SelectPoint(point);
        Assert.Contains("18.5 %", viewModel.LegendText);
    }

    [Fact]
    public async Task Date_range_reload_filters_chart_points()
    {
        var profile = TestData.Profile();
        var repository = new FakeMeasurementRepository();
        repository.Items.Add(CreateMeasurement(profile.Id, 80m, new DateTimeOffset(2026, 9, 6, 8, 0, 0, TimeSpan.Zero)));
        repository.Items.Add(CreateMeasurement(profile.Id, 81m, new DateTimeOffset(2026, 9, 7, 8, 0, 0, TimeSpan.Zero)));
        repository.Items.Add(CreateMeasurement(profile.Id, 82m, new DateTimeOffset(2026, 9, 8, 8, 0, 0, TimeSpan.Zero)));
        var viewModel = CreateViewModel(repository, profile.Id);

        viewModel.UseFromDate = true;
        viewModel.UseToDate = true;
        viewModel.FromDate = new DateTime(2026, 9, 7);
        viewModel.ToDate = new DateTime(2026, 9, 8);
        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(2, viewModel.Points.Count);
        Assert.Equal([81m, 82m], viewModel.Points.Select(point => point.WeightKg));
    }

    [Fact]
    public async Task Load_shows_empty_state_when_no_measurements_exist()
    {
        var viewModel = CreateViewModel(new FakeMeasurementRepository(), TestData.Profile().Id);

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsEmpty);
        Assert.Empty(viewModel.Points);
        Assert.Null(viewModel.ErrorMessage);
    }

    [Fact]
    public async Task Load_translates_repository_failure_to_a_recoverable_error()
    {
        var viewModel = CreateViewModel(new ThrowingMeasurementRepository(), TestData.Profile().Id);

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.False(viewModel.IsEmpty);
        Assert.Equal("We couldn't load this chart. Try again.", viewModel.ErrorMessage);
    }

    [Fact]
    public void Axis_labels_use_the_selected_language()
    {
        var languageService = TestData.LanguageService();
        languageService.SetLanguage("de");
        var viewModel = CreateViewModel(new FakeMeasurementRepository(), TestData.Profile().Id, languageService);

        Assert.Equal("Gewicht", viewModel.WeightAxisLabel);
        Assert.Equal("Datum", viewModel.DateAxisLabel);
    }

    private static WeightGraphicViewModel CreateViewModel(
        IMeasurementRepository repository,
        Anthropometry.Domain.Profiles.ProfileId profileId,
        Anthropometry.App.Localization.LanguageService? languageService = null,
        DisplayPreferencesService? displayPreferences = null,
        FakeCalculationResultRepository? resultRepository = null,
        EntitlementSnapshot? entitlement = null)
        => new(
            new GetMetricHistory(repository, resultRepository ?? new FakeCalculationResultRepository()),
            profileId,
            languageService ?? TestData.LanguageService(),
            displayPreferences ?? TestData.DisplayPreferences(),
            entitlement);

    private static Measurement CreateMeasurement(
        Anthropometry.Domain.Profiles.ProfileId profileId,
        decimal weightKg,
        DateTimeOffset measuredAtUtc)
        => Measurement.Create(
            profileId,
            new MeasurementInput(
                MeasurementType.WeightAndSizes,
                weightKg,
                180m,
                40m,
                90m,
                35,
                ActivityLevel.Moderate,
                measuredAtUtc),
            measuredAtUtc).Value;

    private static CalculationResult CreateResult(
        Measurement measurement,
        CalculationType type,
        decimal value,
        string unit)
        => CalculationResult.Create(
            measurement.Id,
            type,
            new CalculationResultValue(value, unit, "test-formula", "1.0"),
            measurement.MeasuredAtUtc.AddMinutes(1)).Value;
}
