using Anthropometry.Application.Calculations;
using Anthropometry.Application.Tests.Support;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Calculations.BodyFat;
using Anthropometry.Domain.Calculations.Bmr;
using Anthropometry.Domain.Calculations.Tdee;
using Anthropometry.Domain.Measurements;

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

    [Theory]
    [InlineData("body-fat")]
    [InlineData("bmr")]
    [InlineData("tdee")]
    public async Task Calculations_reject_weight_only_measurements(string calculation)
    {
        var profile = TestData.Profile();
        var measurement = Measurement.Create(
            profile.Id,
            new MeasurementInput(MeasurementType.WeightOnly, 79m, 180m, null, null, 35, ActivityLevel.Moderate, DateTimeOffset.UtcNow),
            DateTimeOffset.UtcNow).Value;
        var measurements = new FakeMeasurementRepository();
        var results = new FakeCalculationResultRepository();
        measurements.Items.Add(measurement);
        var catalog = new FormulaCatalog(new UsNavyMaleBodyFatFormula(), new MifflinStJeorMaleBmrFormula(), new TdeeFormula());

        var result = calculation switch
        {
            "body-fat" => await new CalculateBodyFat(measurements, results, catalog, new FakeClock()).ExecuteAsync(new CalculateBodyFatCommand(profile.Id, measurement.Id), CancellationToken.None),
            "bmr" => await new CalculateBasalMetabolicRate(measurements, results, catalog, new FakeClock()).ExecuteAsync(new CalculateBmrCommand(profile.Id, measurement.Id), CancellationToken.None),
            _ => await new CalculateTotalDailyEnergyExpenditure(measurements, results, catalog, new FakeClock()).ExecuteAsync(new CalculateTdeeCommand(profile.Id, measurement.Id), CancellationToken.None)
        };

        Assert.False(result.IsSuccess);
        Assert.Equal("calculation.measurementType.unavailable", result.Error!.Code);
        Assert.Empty(results.Items);
    }

    [Theory]
    [InlineData("body-fat")]
    [InlineData("bmr")]
    [InlineData("tdee")]
    public async Task Calculations_use_previous_sizes_for_weight_only_measurements(string calculation)
    {
        var profile = TestData.Profile();
        var previous = TestData.Measurement(profile.Id);
        var measurement = Measurement.Create(
            profile.Id,
            new MeasurementInput(MeasurementType.WeightOnly, 79m, 180m, null, null, 35, ActivityLevel.Moderate, DateTimeOffset.UtcNow),
            DateTimeOffset.UtcNow).Value;
        var measurements = new FakeMeasurementRepository();
        measurements.Items.Add(previous);
        measurements.Items.Add(measurement);
        var results = new FakeCalculationResultRepository();
        var catalog = new FormulaCatalog(new UsNavyMaleBodyFatFormula(), new MifflinStJeorMaleBmrFormula(), new TdeeFormula());

        var result = calculation switch
        {
            "body-fat" => await new CalculateBodyFat(measurements, results, catalog, new FakeClock()).ExecuteAsync(new CalculateBodyFatCommand(profile.Id, measurement.Id), CancellationToken.None),
            "bmr" => await new CalculateBasalMetabolicRate(measurements, results, catalog, new FakeClock()).ExecuteAsync(new CalculateBmrCommand(profile.Id, measurement.Id), CancellationToken.None),
            _ => await new CalculateTotalDailyEnergyExpenditure(measurements, results, catalog, new FakeClock()).ExecuteAsync(new CalculateTdeeCommand(profile.Id, measurement.Id), CancellationToken.None)
        };

        Assert.True(result.IsSuccess);
        Assert.Equal(measurement.Id, result.Value.MeasurementId);
        Assert.Single(results.Items);
    }
}
