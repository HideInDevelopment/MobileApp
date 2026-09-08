using Anthropometry.Application.Calculations;
using Anthropometry.Application.Tests.Support;
using Anthropometry.Domain.Calculations.BodyFat;
using Anthropometry.Domain.Calculations.Bmr;
using Anthropometry.Domain.Calculations.Tdee;

namespace Anthropometry.Application.Tests.Calculations;

public sealed class CalculationUseCaseTests
{
    [Fact]
    public async Task Body_fat_calculation_persists_formula_identity_and_version()
    {
        var profile = TestData.Profile();
        var measurement = TestData.Measurement(profile.Id);
        var measurements = new FakeMeasurementRepository();
        var results = new FakeCalculationResultRepository();
        measurements.Items.Add(measurement);
        var catalog = new FormulaCatalog(new UsNavyMaleBodyFatFormula(), new MifflinStJeorMaleBmrFormula(), new TdeeFormula());

        var result = await new CalculateBodyFat(measurements, results, catalog, new FakeClock())
            .ExecuteAsync(new CalculateBodyFatCommand(profile.Id, measurement.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("us-navy-male-body-fat", result.Value.FormulaId);
        Assert.Equal("1.0", result.Value.FormulaVersion);
        Assert.Equal(result.Value.FormulaId, results.Items[0].FormulaId);
        Assert.Equal(result.Value.FormulaVersion, results.Items[0].FormulaVersion);
    }

    [Fact]
    public async Task Calculation_returns_not_found_for_missing_measurement()
    {
        var results = new FakeCalculationResultRepository();
        var catalog = new FormulaCatalog(new UsNavyMaleBodyFatFormula(), new MifflinStJeorMaleBmrFormula(), new TdeeFormula());

        var result = await new CalculateBasalMetabolicRate(new FakeMeasurementRepository(), results, catalog, new FakeClock())
            .ExecuteAsync(new CalculateBmrCommand(Anthropometry.Domain.Profiles.ProfileId.New(), Anthropometry.Domain.Measurements.MeasurementId.New()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("measurement.notFound", result.Error!.Code);
        Assert.Empty(results.Items);
    }

    [Fact]
    public async Task Tdee_calculation_uses_measurement_activity_level()
    {
        var profile = TestData.Profile();
        var measurement = TestData.Measurement(profile.Id);
        var measurements = new FakeMeasurementRepository();
        var results = new FakeCalculationResultRepository();
        measurements.Items.Add(measurement);
        var catalog = new FormulaCatalog(new UsNavyMaleBodyFatFormula(), new MifflinStJeorMaleBmrFormula(), new TdeeFormula());

        var result = await new CalculateTotalDailyEnergyExpenditure(measurements, results, catalog, new FakeClock())
            .ExecuteAsync(new CalculateTdeeCommand(profile.Id, measurement.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1755m * 1.55m, result.Value.Value);
        Assert.Equal("tdee-activity-multiplier", result.Value.FormulaId);
    }
}
