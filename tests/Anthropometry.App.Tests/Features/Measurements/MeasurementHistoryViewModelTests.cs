using Anthropometry.App.Features.Measurements;
using Anthropometry.App.Display;
using Anthropometry.Application.Measurements;
using Anthropometry.Application.Entitlements;
using Anthropometry.Application.Common;
using Anthropometry.App.Tests.Support;
using Anthropometry.Domain.Measurements;
using Xunit;

namespace Anthropometry.App.Tests.Features.Measurements;

public sealed class MeasurementHistoryViewModelTests
{
    [Fact]
    public async Task Load_shows_empty_state_when_no_measurements_exist()
    {
        var profile = TestData.Profile();
        var viewModel = CreateViewModel(new FakeMeasurementRepository(), out _, profile);

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
        var viewModel = CreateViewModel(repository, out _, profile);

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
        var viewModel = CreateViewModel(repository, out _, profile);

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
        var viewModel = CreateViewModel(repository, out _, profile, languageService);

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
        var viewModel = CreateViewModel(repository, out _, profile, displayPreferences: displayPreferences, entitlement: EntitlementTestData.Premium);

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
        var viewModel = CreateViewModel(repository, out var navigation, profile);

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
        var viewModel = CreateViewModel(repository, out var navigation, profile);

        await viewModel.LoadCommand.ExecuteAsync(null);
        await viewModel.SelectCommand.ExecuteAsync(viewModel.Measurements[0]);

        Assert.True(viewModel.Measurements[0].CanViewResults);
        Assert.False(viewModel.Measurements[0].ShowWarningIcon);
        Assert.Equal(viewModel.Measurements[0].Measurement.Id, navigation.SelectedMeasurementId);
    }

    [Fact]
    public void Charts_command_toggles_the_chart_options_menu()
    {
        var profile = TestData.Profile();
        var viewModel = CreateViewModel(new FakeMeasurementRepository(), out var navigation, profile);

        viewModel.ChartsCommand.Execute(null);

        Assert.True(viewModel.IsChartMenuVisible);
        Assert.Null(navigation.WeightGraphicProfileId);

        viewModel.ChartsCommand.Execute(null);

        Assert.False(viewModel.IsChartMenuVisible);
    }

    [Fact]
    public void Measurement_type_selector_command_updates_the_filter()
    {
        var viewModel = CreateViewModel(new FakeMeasurementRepository(), out _, TestData.Profile());

        viewModel.SelectMeasurementTypeCommand.Execute(MeasurementType.WeightOnly.ToString());

        Assert.Equal(MeasurementType.WeightOnly, viewModel.SelectedTypeOption!.Value);
    }

    [Fact]
    public async Task Selecting_weight_graphic_closes_the_menu_and_opens_the_graphic()
    {
        var profile = TestData.Profile();
        var viewModel = CreateViewModel(new FakeMeasurementRepository(), out var navigation, profile);

        viewModel.ChartsCommand.Execute(null);
        await viewModel.WeightGraphicCommand.ExecuteAsync(null);

        Assert.False(viewModel.IsChartMenuVisible);
        Assert.Equal(profile.Id, navigation.WeightGraphicProfileId);
    }

    [Fact]
    public async Task Edit_command_delegates_the_profile_and_measurement()
    {
        var profile = TestData.Profile();
        var repository = new FakeMeasurementRepository();
        repository.Items.Add(CreateMeasurement(profile.Id, MeasurementType.WeightAndSizes, DateTimeOffset.UtcNow));
        var viewModel = CreateViewModel(repository, out var navigation, profile);

        await viewModel.LoadCommand.ExecuteAsync(null);
        await viewModel.EditCommand.ExecuteAsync(viewModel.Measurements[0]);

        Assert.Equal(profile.Id, navigation.EditProfileId);
        Assert.Equal(viewModel.Measurements[0].Measurement.Id, navigation.EditMeasurementId);
    }

    [Fact]
    public async Task Delete_command_confirms_then_removes_the_row_and_reloads_history()
    {
        var profile = TestData.Profile();
        var repository = new FakeMeasurementRepository();
        repository.Items.Add(CreateMeasurement(profile.Id, MeasurementType.WeightAndSizes, DateTimeOffset.UtcNow));
        var viewModel = CreateViewModel(repository, out var navigation, profile);
        navigation.ConfirmDeleteResult = true;

        await viewModel.LoadCommand.ExecuteAsync(null);
        await viewModel.DeleteCommand.ExecuteAsync(viewModel.Measurements[0]);

        Assert.True(navigation.ConfirmDeleteCalled);
        Assert.Empty(repository.Items);
        Assert.Empty(viewModel.Measurements);
        Assert.True(viewModel.IsEmpty);
    }

    [Fact]
    public async Task Delete_command_does_not_remove_the_row_when_cancelled()
    {
        var profile = TestData.Profile();
        var repository = new FakeMeasurementRepository();
        repository.Items.Add(CreateMeasurement(profile.Id, MeasurementType.WeightAndSizes, DateTimeOffset.UtcNow));
        var viewModel = CreateViewModel(repository, out var navigation, profile);

        await viewModel.LoadCommand.ExecuteAsync(null);
        await viewModel.DeleteCommand.ExecuteAsync(viewModel.Measurements[0]);

        Assert.True(navigation.ConfirmDeleteCalled);
        Assert.Single(repository.Items);
        Assert.Single(viewModel.Measurements);
    }

    [Fact]
    public async Task Filters_reload_history_and_keep_edit_delete_actions_available()
    {
        var profile = TestData.Profile();
        var repository = new FakeMeasurementRepository();
        repository.Items.Add(CreateMeasurement(profile.Id, MeasurementType.WeightAndSizes, DateTimeOffset.UtcNow.AddDays(-1)));
        repository.Items.Add(CreateMeasurement(profile.Id, MeasurementType.WeightOnly, DateTimeOffset.UtcNow));
        var viewModel = CreateViewModel(repository, out _, profile);

        viewModel.SelectedType = MeasurementType.WeightOnly;
        await viewModel.LoadCommand.ExecuteAsync(null);

        var item = Assert.Single(viewModel.Measurements);
        Assert.Equal(MeasurementType.WeightOnly, item.Measurement.Type);
        Assert.NotNull(viewModel.EditCommand);
        Assert.NotNull(viewModel.DeleteCommand);
        Assert.True(viewModel.HasActiveFilters);
    }

    [Fact]
    public async Task Reversed_dates_show_a_recoverable_validation_message()
    {
        var profile = TestData.Profile();
        var viewModel = CreateViewModel(new FakeMeasurementRepository(), out _, profile);
        viewModel.UseFromDate = true;
        viewModel.UseToDate = true;
        viewModel.FromDate = new DateTime(2026, 9, 10);
        viewModel.ToDate = new DateTime(2026, 9, 9);

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal("Choose a start date on or before the end date.", viewModel.ErrorMessage);
        Assert.False(viewModel.IsNoMatch);
    }

    [Fact]
    public async Task Clear_filters_restores_the_unfiltered_history_and_empty_copy_is_distinct()
    {
        var profile = TestData.Profile();
        var repository = new FakeMeasurementRepository();
        repository.Items.Add(CreateMeasurement(profile.Id, MeasurementType.WeightAndSizes, DateTimeOffset.UtcNow));
        var viewModel = CreateViewModel(repository, out _, profile);
        viewModel.SelectedType = MeasurementType.WeightOnly;
        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsNoMatch);
        Assert.Equal("No matching measurements", viewModel.EmptyStateTitle);

        await viewModel.ClearFiltersCommand.ExecuteAsync(null);

        Assert.False(viewModel.HasActiveFilters);
        Assert.False(viewModel.IsNoMatch);
        Assert.Single(viewModel.Measurements);
    }

    private static MeasurementHistoryViewModel CreateViewModel(
        FakeMeasurementRepository repository,
        out NavigationSpy navigation,
        Anthropometry.Domain.Profiles.Profile profile,
        Anthropometry.App.Localization.LanguageService? languageService = null,
        DisplayPreferencesService? displayPreferences = null,
        EntitlementSnapshot? entitlement = null)
    {
        navigation = new NavigationSpy();
        return new MeasurementHistoryViewModel(
            new GetMeasurementHistory(repository),
            new DeleteMeasurement(repository),
            new ProfileDto(
                profile.Id,
                profile.Name,
                new ProfileSettingsDto(180m, 35, Anthropometry.Domain.Calculations.ActivityLevel.Moderate),
                profile.CreatedAtUtc,
                profile.UpdatedAtUtc,
                profile.Gender),
            navigation,
            languageService ?? TestData.LanguageService(),
            displayPreferences ?? TestData.DisplayPreferences(),
            entitlement);
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

        public Anthropometry.Domain.Profiles.ProfileId? WeightGraphicProfileId { get; private set; }

        public Anthropometry.Domain.Profiles.ProfileId? EditProfileId { get; private set; }

        public Anthropometry.Domain.Measurements.MeasurementId? EditMeasurementId { get; private set; }

        public bool ConfirmDeleteResult { get; set; }

        public bool ConfirmDeleteCalled { get; private set; }

        public Task ShowResultsAsync(Anthropometry.Application.Common.MeasurementDto measurement)
        {
            SelectedMeasurementId = measurement.Id;
            return Task.CompletedTask;
        }

        public Task ShowResultsAsync(Anthropometry.Application.Common.ProfileDto profile, Anthropometry.Application.Common.MeasurementDto measurement)
        {
            SelectedMeasurementId = measurement.Id;
            return Task.CompletedTask;
        }

        public Task EditMeasurementAsync(Anthropometry.Application.Common.ProfileDto profile, Anthropometry.Application.Common.MeasurementDto measurement)
        {
            EditProfileId = profile.Id;
            EditMeasurementId = measurement.Id;
            return Task.CompletedTask;
        }

        public Task<bool> ConfirmDeleteAsync(Anthropometry.Application.Common.MeasurementDto measurement)
        {
            ConfirmDeleteCalled = true;
            return Task.FromResult(ConfirmDeleteResult);
        }

        public Task ShowHistoryAsync(Anthropometry.Application.Common.ProfileDto profile) => Task.CompletedTask;

        public Task ShowWeightGraphicAsync(Anthropometry.Domain.Profiles.ProfileId profileId)
        {
            WeightGraphicProfileId = profileId;
            return Task.CompletedTask;
        }

        public Task CloseMeasurementAsync() => Task.CompletedTask;

        public Task CancelAsync() => Task.CompletedTask;

        public Task ShowGuidanceAsync(Anthropometry.App.Features.Help.GuidanceTopic topic) => Task.CompletedTask;
    }
}
