using Anthropometry.App.Features.Measurements;
using Anthropometry.App.Display;
using Anthropometry.App.Tests.Support;
using Anthropometry.Application.Abstractions;
using Anthropometry.Application.Measurements;
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
        displayPreferences.SetWeightUnit(DisplayPreferencesService.PoundsCode);
        var viewModel = CreateViewModel(repository, profile.Id, displayPreferences: displayPreferences);

        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.SelectPoint(viewModel.Points[0]);

        Assert.Equal("09/08", viewModel.Points[0].DateText);
        Assert.Equal(176.369809744m, viewModel.Points[0].DisplayedWeight, 9);
        Assert.Equal(154.323583526m, viewModel.ChartMinimumWeight, 9);
        Assert.Equal(198.416035962m, viewModel.ChartMaximumWeight, 9);
        Assert.Contains("176.37 lb", viewModel.LegendText);
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
        Assert.Equal("We couldn't load the weight graphic. Try again.", viewModel.ErrorMessage);
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
        DisplayPreferencesService? displayPreferences = null)
        => new(
            new GetMeasurementHistory(repository),
            profileId,
            languageService ?? TestData.LanguageService(),
            displayPreferences ?? TestData.DisplayPreferences());

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
}
