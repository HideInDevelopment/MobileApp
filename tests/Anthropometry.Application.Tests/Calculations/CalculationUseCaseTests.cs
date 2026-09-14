using Anthropometry.Application.Calculations;
using Anthropometry.Application.Tests.Support;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Calculations.BodyFat;
using Anthropometry.Domain.Calculations.Bmr;
using Anthropometry.Domain.Calculations.Tdee;
using Anthropometry.Domain.Measurements;
using Anthropometry.Domain.Profiles;

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
        var catalog = new FormulaCatalog(
            new UsNavyMaleBodyFatFormula(),
            new MifflinStJeorMaleBmrFormula(),
            new TdeeFormula(),
            new UsNavyFemaleBodyFatFormula(),
            new MifflinStJeorFemaleBmrFormula());

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
        var catalog = new FormulaCatalog(
            new UsNavyMaleBodyFatFormula(),
            new MifflinStJeorMaleBmrFormula(),
            new TdeeFormula(),
            new UsNavyFemaleBodyFatFormula(),
            new MifflinStJeorFemaleBmrFormula());

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
        var catalog = new FormulaCatalog(
            new UsNavyMaleBodyFatFormula(),
            new MifflinStJeorMaleBmrFormula(),
            new TdeeFormula(),
            new UsNavyFemaleBodyFatFormula(),
            new MifflinStJeorFemaleBmrFormula());

        var result = await new CalculateTotalDailyEnergyExpenditure(measurements, results, catalog, new FakeClock())
            .ExecuteAsync(new CalculateTdeeCommand(profile.Id, measurement.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1755m * 1.55m, result.Value.Value);
        Assert.Equal("tdee-activity-multiplier", result.Value.FormulaId);
    }

    [Fact]
    public async Task Female_calculations_select_female_formulas_and_persist_their_identities()
    {
        var profile = Profile.Create(
            "Anna",
            ProfileSettings.Create(180m, 35, ActivityLevel.Moderate).Value,
            DateTimeOffset.UtcNow,
            ProfileGender.Female).Value;
        var measurement = Measurement.Create(
            profile.Id,
            new MeasurementInput(
                MeasurementType.WeightAndSizes,
                80m,
                180m,
                40m,
                90m,
                35,
                ActivityLevel.Moderate,
                DateTimeOffset.UtcNow,
                110m,
                ProfileGender.Female),
            DateTimeOffset.UtcNow).Value;
        var measurements = new FakeMeasurementRepository();
        measurements.Items.Add(measurement);
        var results = new FakeCalculationResultRepository();
        var catalog = new FormulaCatalog(
            new UsNavyMaleBodyFatFormula(),
            new MifflinStJeorMaleBmrFormula(),
            new TdeeFormula(),
            new UsNavyFemaleBodyFatFormula(),
            new MifflinStJeorFemaleBmrFormula());

        var bodyFat = await new CalculateBodyFat(measurements, results, catalog, new FakeClock())
            .ExecuteAsync(new CalculateBodyFatCommand(profile.Id, measurement.Id), CancellationToken.None);
        var bmr = await new CalculateBasalMetabolicRate(measurements, results, catalog, new FakeClock())
            .ExecuteAsync(new CalculateBmrCommand(profile.Id, measurement.Id), CancellationToken.None);
        var tdee = await new CalculateTotalDailyEnergyExpenditure(measurements, results, catalog, new FakeClock())
            .ExecuteAsync(new CalculateTdeeCommand(profile.Id, measurement.Id), CancellationToken.None);

        Assert.True(bodyFat.IsSuccess);
        Assert.Equal("us-navy-female-body-fat", bodyFat.Value.FormulaId);
        Assert.True(bmr.IsSuccess);
        Assert.Equal(1589m, bmr.Value.Value);
        Assert.Equal("mifflin-st-jeor-female-bmr", bmr.Value.FormulaId);
        Assert.True(tdee.IsSuccess);
        Assert.Equal(1589m * 1.55m, tdee.Value.Value);
        Assert.Equal(3, results.Items.Count);
    }

    [Fact]
    public async Task Female_weight_only_calculation_reuses_previous_female_sizes()
    {
        var profile = Profile.Create(
            "Anna",
            ProfileSettings.Create(180m, 35, ActivityLevel.Moderate).Value,
            DateTimeOffset.UtcNow,
            ProfileGender.Female).Value;
        var measuredAt = DateTimeOffset.UtcNow;
        var previous = Measurement.Create(
            profile.Id,
            new MeasurementInput(
                MeasurementType.WeightAndSizes,
                80m,
                180m,
                40m,
                90m,
                35,
                ActivityLevel.Moderate,
                measuredAt.AddMinutes(-1),
                110m,
                ProfileGender.Female),
            measuredAt).Value;
        var weightOnly = Measurement.Create(
            profile.Id,
            new MeasurementInput(
                MeasurementType.WeightOnly,
                79m,
                180m,
                null,
                null,
                35,
                ActivityLevel.Moderate,
                measuredAt,
                Gender: ProfileGender.Female),
            measuredAt).Value;
        var measurements = new FakeMeasurementRepository();
        measurements.Items.Add(previous);
        measurements.Items.Add(weightOnly);
        var results = new FakeCalculationResultRepository();
        var catalog = new FormulaCatalog(
            new UsNavyMaleBodyFatFormula(),
            new MifflinStJeorMaleBmrFormula(),
            new TdeeFormula(),
            new UsNavyFemaleBodyFatFormula(),
            new MifflinStJeorFemaleBmrFormula());

        var result = await new CalculateBodyFat(measurements, results, catalog, new FakeClock())
            .ExecuteAsync(new CalculateBodyFatCommand(profile.Id, weightOnly.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("us-navy-female-body-fat", result.Value.FormulaId);
        Assert.Single(results.Items);
        Assert.Equal(weightOnly.Id, results.Items[0].MeasurementId);
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
        var catalog = new FormulaCatalog(
            new UsNavyMaleBodyFatFormula(),
            new MifflinStJeorMaleBmrFormula(),
            new TdeeFormula(),
            new UsNavyFemaleBodyFatFormula(),
            new MifflinStJeorFemaleBmrFormula());

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
        var catalog = new FormulaCatalog(
            new UsNavyMaleBodyFatFormula(),
            new MifflinStJeorMaleBmrFormula(),
            new TdeeFormula(),
            new UsNavyFemaleBodyFatFormula(),
            new MifflinStJeorFemaleBmrFormula());

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
