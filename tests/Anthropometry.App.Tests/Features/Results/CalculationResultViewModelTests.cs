using Anthropometry.App.Features.Results;
using Anthropometry.App.Features.Measurements;
using Anthropometry.App.Features.Help;
using Anthropometry.Application.Calculations;
using Anthropometry.Application.Common;
using Anthropometry.App.Tests.Support;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Measurements;

namespace Anthropometry.App.Tests.Features.Results;

public sealed class CalculationResultViewModelTests
{
    [Fact]
    public async Task Load_exposes_friendly_titles_and_two_decimal_values()
    {
        var profile = TestData.Profile();
        var measurement = TestData.Measurement(profile.Id);
        var repository = new FakeCalculationResultRepository();
        repository.Items.Add(CreateResult(measurement.Id, CalculationType.BodyFatPercentage, 18.456m, "%", "us-navy-male-body-fat"));
        repository.Items.Add(CreateResult(measurement.Id, CalculationType.BasalMetabolicRate, 1755m, "kcal/day", "mifflin-st-jeor-male-bmr"));
        repository.Items.Add(CreateResult(measurement.Id, CalculationType.TotalDailyEnergyExpenditure, 2720.256m, "kcal/day", "tdee-activity-multiplier"));
        var viewModel = new CalculationResultViewModel(new GetCalculationResults(repository), measurement.Id, MeasurementType.WeightAndSizes, TestData.LanguageService());

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Collection(
            viewModel.Results,
            result =>
            {
                Assert.Equal("Body Fat Percentage", result.Title);
                Assert.Equal(18.456m.ToString("F2", System.Globalization.CultureInfo.CurrentCulture), result.Value);
                Assert.Equal("%", result.Unit);
                Assert.Equal(
                    "This estimates the proportion of your body weight that is fat using your height and circumference measurements.",
                    GetDescription(result));
            },
            result =>
            {
                Assert.Equal("Basal Metabolic Rate", result.Title);
                Assert.Equal(1755m.ToString("F2", System.Globalization.CultureInfo.CurrentCulture), result.Value);
                Assert.Equal("kcal/day", result.Unit);
                Assert.Equal(
                    "This estimates the energy your body uses at rest from your weight, height, age, and profile information.",
                    GetDescription(result));
            },
            result =>
            {
                Assert.Equal("Total Daily Energy Expenditure", result.Title);
                Assert.Equal(2720.256m.ToString("F2", System.Globalization.CultureInfo.CurrentCulture), result.Value);
                Assert.Equal("kcal/day", result.Unit);
                Assert.Equal(
                    "This estimates daily energy needs by combining basal metabolism with your selected activity level.",
                    GetDescription(result));
            });
        Assert.False(viewModel.IsLoading);
        Assert.False(viewModel.ShowWarningIcon);
    }

    [Fact]
    public async Task Load_shows_recoverable_error_when_results_cannot_be_read()
    {
        var measurementId = Anthropometry.Domain.Measurements.MeasurementId.New();
        var viewModel = new CalculationResultViewModel(new GetCalculationResults(new ThrowingCalculationResultRepository()), measurementId, MeasurementType.WeightAndSizes, TestData.LanguageService());

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal("We couldn't load results. Try again.", viewModel.ErrorMessage);
        Assert.Empty(viewModel.Results);
    }

    [Fact]
    public async Task Load_uses_the_selected_language_for_result_titles()
    {
        var profile = TestData.Profile();
        var measurement = TestData.Measurement(profile.Id);
        var repository = new FakeCalculationResultRepository();
        repository.Items.Add(CreateResult(measurement.Id, CalculationType.BodyFatPercentage, 18.456m, "%", "us-navy-male-body-fat"));
        var languageService = TestData.LanguageService();
        languageService.SetLanguage("es");
        var viewModel = new CalculationResultViewModel(
            new GetCalculationResults(repository),
            measurement.Id,
            MeasurementType.WeightAndSizes,
            languageService);

        await viewModel.LoadCommand.ExecuteAsync(null);

        var result = Assert.Single(viewModel.Results);
        Assert.Equal("Porcentaje de grasa corporal", result.Title);
        Assert.Equal(
            "Estima qué proporción de tu peso corporal corresponde a grasa usando tu altura y tus medidas corporales.",
            GetDescription(result));
    }

    [Fact]
    public void Weight_only_results_show_warning()
    {
        var viewModel = new CalculationResultViewModel(
            new GetCalculationResults(new FakeCalculationResultRepository()),
            Anthropometry.Domain.Measurements.MeasurementId.New(),
            MeasurementType.WeightOnly,
            TestData.LanguageService());

        Assert.True(viewModel.ShowWarningIcon);
    }

    [Fact]
    public async Task View_history_command_uses_the_current_profile()
    {
        var profile = TestData.Profile();
        var profileDto = new ProfileDto(
            profile.Id,
            profile.Name,
            new ProfileSettingsDto(180m, 35, ActivityLevel.Moderate),
            profile.CreatedAtUtc,
            profile.UpdatedAtUtc);
        var navigation = new NavigationSpy();
        var viewModel = new CalculationResultViewModel(
            new GetCalculationResults(new FakeCalculationResultRepository()),
            Anthropometry.Domain.Measurements.MeasurementId.New(),
            MeasurementType.WeightAndSizes,
            TestData.LanguageService(),
            profileDto,
            navigation);

        Assert.True(viewModel.CanViewHistory);
        await viewModel.ViewHistoryCommand.ExecuteAsync(null);

        Assert.Same(profileDto, navigation.HistoryProfile);
    }

    private static string? GetDescription(CalculationResultDisplayItem result)
        => result.GetType().GetProperty("Description")?.GetValue(result) as string;

    private static CalculationResult CreateResult(Anthropometry.Domain.Measurements.MeasurementId measurementId, CalculationType type, decimal value, string unit, string formulaId)
        => CalculationResult.Create(measurementId, type, new CalculationResultValue(value, unit, formulaId, "1.0"), DateTimeOffset.UtcNow).Value;

    private sealed class NavigationSpy : IMeasurementNavigation
    {
        public ProfileDto? HistoryProfile { get; private set; }

        public Task ShowResultsAsync(MeasurementDto measurement) => Task.CompletedTask;

        public Task ShowResultsAsync(ProfileDto profile, MeasurementDto measurement) => Task.CompletedTask;

        public Task EditMeasurementAsync(ProfileDto profile, MeasurementDto measurement) => Task.CompletedTask;

        public Task<bool> ConfirmDeleteAsync(MeasurementDto measurement) => Task.FromResult(false);

        public Task ShowHistoryAsync(ProfileDto profile)
        {
            HistoryProfile = profile;
            return Task.CompletedTask;
        }

        public Task ShowChartOptionsAsync(Anthropometry.Domain.Profiles.ProfileId profileId) => Task.CompletedTask;

        public Task CloseMeasurementAsync() => Task.CompletedTask;

        public Task CancelAsync() => Task.CompletedTask;

        public Task ShowGuidanceAsync(GuidanceTopic topic) => Task.CompletedTask;
    }
}
