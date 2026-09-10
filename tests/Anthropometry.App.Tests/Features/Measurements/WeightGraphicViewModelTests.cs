using Anthropometry.App.Features.Measurements;
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
        Anthropometry.App.Localization.LanguageService? languageService = null)
        => new(
            new GetMeasurementHistory(repository),
            profileId,
            languageService ?? TestData.LanguageService());

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
