using Anthropometry.App.Features.Measurements;
using Anthropometry.App.Display;
using Anthropometry.Application.Measurements;
using Anthropometry.App.Tests.Support;
using Anthropometry.Domain.Measurements;
using Xunit;

namespace Anthropometry.App.Tests.Features.Measurements;

public sealed class MeasurementHistoryViewModelTests
{
    [Fact]
    public async Task Load_shows_empty_state_when_no_measurements_exist()
    {
        var viewModel = CreateViewModel(new FakeMeasurementRepository(), out _, TestData.Profile().Id);

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsEmpty);
        Assert.Empty(viewModel.Measurements);
        Assert.Null(viewModel.ErrorMessage);
    }

    [Fact]
    public async Task Load_formats_date_and_measurement_type_for_people()
    {
        var profile = TestData.Profile();
        var repository = new FakeMeasurementRepository();
        repository.Items.Add(CreateMeasurement(profile.Id, MeasurementType.WeightAndSizes, new DateTimeOffset(2026, 9, 7, 18, 30, 0, TimeSpan.Zero)));
        repository.Items.Add(CreateMeasurement(profile.Id, MeasurementType.WeightOnly, new DateTimeOffset(2026, 9, 8, 8, 15, 0, TimeSpan.Zero)));
        var viewModel = CreateViewModel(repository, out _, profile.Id);

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(2, viewModel.Measurements.Count);
        Assert.Equal("08/09/2026", viewModel.Measurements[0].DateText);
        Assert.Equal("Weight only", viewModel.Measurements[0].MeasurementTypeText);
        Assert.True(viewModel.Measurements[0].ShowWarningIcon);
        Assert.Equal("07/09/2026", viewModel.Measurements[1].DateText);
        Assert.Equal("Weight and sizes", viewModel.Measurements[1].MeasurementTypeText);
        Assert.False(viewModel.Measurements[1].ShowWarningIcon);
    }

    [Fact]
    public async Task Load_exposes_a_background_color_for_each_history_row()
    {
        var profile = TestData.Profile();
        var repository = new FakeMeasurementRepository();
        repository.Items.Add(CreateMeasurement(profile.Id, MeasurementType.WeightAndSizes, new DateTimeOffset(2026, 9, 7, 18, 30, 0, TimeSpan.Zero)));
        repository.Items.Add(CreateMeasurement(profile.Id, MeasurementType.WeightOnly, new DateTimeOffset(2026, 9, 8, 8, 15, 0, TimeSpan.Zero)));
        var viewModel = CreateViewModel(repository, out _, profile.Id);

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal("#FFF2CC", viewModel.Measurements[0].RowBackgroundColor);
        Assert.Equal("Transparent", viewModel.Measurements[1].RowBackgroundColor);
    }

    [Fact]
    public async Task Load_uses_the_selected_language_for_history_labels()
    {
        var profile = TestData.Profile();
        var repository = new FakeMeasurementRepository();
        repository.Items.Add(CreateMeasurement(profile.Id, MeasurementType.WeightOnly, DateTimeOffset.UtcNow));
        var languageService = TestData.LanguageService();
        languageService.SetLanguage("es");
        var viewModel = CreateViewModel(repository, out _, profile.Id, languageService);

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal("Solo peso", viewModel.Measurements[0].MeasurementTypeText);
        Assert.Equal("Peso: 80 kg", viewModel.Measurements[0].WeightText);
        Assert.Equal("Altura: 1,8 m", viewModel.Measurements[0].HeightText);
    }

    [Fact]
    public async Task Load_formats_history_values_using_selected_date_and_units()
    {
        var profile = TestData.Profile();
        var repository = new FakeMeasurementRepository();
        repository.Items.Add(CreateMeasurement(profile.Id, MeasurementType.WeightAndSizes, new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero)));
        var displayPreferences = TestData.DisplayPreferences();
        displayPreferences.SetDateFormat(DisplayPreferencesService.MonthDayYearCode);
        displayPreferences.SetMeasurementSystem(DisplayPreferencesService.ImperialCode);
        var viewModel = CreateViewModel(repository, out _, profile.Id, displayPreferences: displayPreferences);

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal("09/08/2026", viewModel.Measurements[0].DateText);
        Assert.Equal("Weight: 176.37 lb", viewModel.Measurements[0].WeightText);
        Assert.Equal("Height: 5.91 ft", viewModel.Measurements[0].HeightText);
    }

    [Fact]
    public async Task Weight_only_history_item_opens_results()
    {
        var profile = TestData.Profile();
        var repository = new FakeMeasurementRepository();
        repository.Items.Add(CreateMeasurement(profile.Id, MeasurementType.WeightOnly, DateTimeOffset.UtcNow));
        var viewModel = CreateViewModel(repository, out var navigation, profile.Id);

        await viewModel.LoadCommand.ExecuteAsync(null);
        await viewModel.SelectCommand.ExecuteAsync(viewModel.Measurements[0]);

        Assert.True(viewModel.Measurements[0].CanViewResults);
        Assert.True(viewModel.Measurements[0].ShowWarningIcon);
        Assert.Equal(viewModel.Measurements[0].Measurement.Id, navigation.SelectedMeasurementId);
    }

    [Fact]
    public async Task Extended_history_item_opens_results()
    {
        var profile = TestData.Profile();
        var repository = new FakeMeasurementRepository();
        repository.Items.Add(CreateMeasurement(profile.Id, MeasurementType.WeightAndSizes, DateTimeOffset.UtcNow));
        var viewModel = CreateViewModel(repository, out var navigation, profile.Id);

        await viewModel.LoadCommand.ExecuteAsync(null);
        await viewModel.SelectCommand.ExecuteAsync(viewModel.Measurements[0]);

        Assert.True(viewModel.Measurements[0].CanViewResults);
        Assert.False(viewModel.Measurements[0].ShowWarningIcon);
        Assert.Equal(viewModel.Measurements[0].Measurement.Id, navigation.SelectedMeasurementId);
    }

    [Fact]
    public async Task Charts_command_opens_chart_options_for_the_current_profile()
    {
        var profile = TestData.Profile();
        var viewModel = CreateViewModel(new FakeMeasurementRepository(), out var navigation, profile.Id);

        await viewModel.ChartsCommand.ExecuteAsync(null);

        Assert.Equal(profile.Id, navigation.ChartOptionsProfileId);
    }

    private static MeasurementHistoryViewModel CreateViewModel(
        FakeMeasurementRepository repository,
        out NavigationSpy navigation,
        Anthropometry.Domain.Profiles.ProfileId profileId,
        Anthropometry.App.Localization.LanguageService? languageService = null,
        DisplayPreferencesService? displayPreferences = null)
    {
        navigation = new NavigationSpy();
        return new MeasurementHistoryViewModel(
            new GetMeasurementHistory(repository),
            profileId,
            navigation,
            languageService ?? TestData.LanguageService(),
            displayPreferences ?? TestData.DisplayPreferences());
    }

    private static Measurement CreateMeasurement(Anthropometry.Domain.Profiles.ProfileId profileId, MeasurementType type, DateTimeOffset measuredAtUtc)
        => Measurement.Create(
            profileId,
            type == MeasurementType.WeightOnly
                ? new MeasurementInput(type, 80m, 180m, null, null, 35, Anthropometry.Domain.Calculations.ActivityLevel.Moderate, measuredAtUtc)
                : new MeasurementInput(type, 80m, 180m, 40m, 90m, 35, Anthropometry.Domain.Calculations.ActivityLevel.Moderate, measuredAtUtc),
            measuredAtUtc).Value;

    private sealed class NavigationSpy : IMeasurementNavigation
    {
        public Anthropometry.Domain.Measurements.MeasurementId? SelectedMeasurementId { get; private set; }

        public Anthropometry.Domain.Profiles.ProfileId? ChartOptionsProfileId { get; private set; }

        public Task ShowResultsAsync(Anthropometry.Application.Common.MeasurementDto measurement)
        {
            SelectedMeasurementId = measurement.Id;
            return Task.CompletedTask;
        }

        public Task ShowHistoryAsync(Anthropometry.Application.Common.ProfileDto profile) => Task.CompletedTask;

        public Task ShowChartOptionsAsync(Anthropometry.Domain.Profiles.ProfileId profileId)
        {
            ChartOptionsProfileId = profileId;
            return Task.CompletedTask;
        }

        public Task CloseMeasurementAsync() => Task.CompletedTask;

        public Task CancelAsync() => Task.CompletedTask;

        public Task ShowGuidanceAsync(Anthropometry.App.Features.Help.GuidanceTopic topic) => Task.CompletedTask;
    }
}
