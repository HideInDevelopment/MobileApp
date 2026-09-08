using Anthropometry.App.Features.Results;
using Anthropometry.Application.Calculations;
using Anthropometry.App.Tests.Support;
using Anthropometry.Domain.Calculations;

namespace Anthropometry.App.Tests.Features.Results;

public sealed class CalculationResultViewModelTests
{
    [Fact]
    public async Task Load_exposes_all_results_and_formula_versions()
    {
        var profile = TestData.Profile();
        var measurement = TestData.Measurement(profile.Id);
        var repository = new FakeCalculationResultRepository();
        repository.Items.Add(CreateResult(measurement.Id, CalculationType.BodyFatPercentage, 18.46m, "%", "us-navy-male-body-fat"));
        repository.Items.Add(CreateResult(measurement.Id, CalculationType.BasalMetabolicRate, 1755m, "kcal/day", "mifflin-st-jeor-male-bmr"));
        repository.Items.Add(CreateResult(measurement.Id, CalculationType.TotalDailyEnergyExpenditure, 2720.25m, "kcal/day", "tdee-activity-multiplier"));
        var viewModel = new CalculationResultViewModel(new GetCalculationResults(repository), measurement.Id);

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(3, viewModel.Results.Count);
        Assert.Contains("1.0", viewModel.FormulaDetails);
        Assert.False(viewModel.IsLoading);
    }

    [Fact]
    public async Task Load_shows_recoverable_error_when_results_cannot_be_read()
    {
        var measurementId = Anthropometry.Domain.Measurements.MeasurementId.New();
        var viewModel = new CalculationResultViewModel(new GetCalculationResults(new ThrowingCalculationResultRepository()), measurementId);

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal("We couldn't load results. Try again.", viewModel.ErrorMessage);
        Assert.Empty(viewModel.Results);
    }

    private static CalculationResult CreateResult(Anthropometry.Domain.Measurements.MeasurementId measurementId, CalculationType type, decimal value, string unit, string formulaId)
        => CalculationResult.Create(measurementId, type, new CalculationResultValue(value, unit, formulaId, "1.0"), DateTimeOffset.UtcNow).Value;
}
