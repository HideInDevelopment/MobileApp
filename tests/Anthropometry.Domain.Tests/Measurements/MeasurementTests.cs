using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Common;
using Anthropometry.Domain.Measurements;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.Domain.Tests.Measurements;

public sealed class MeasurementTests
{
    [Fact]
    public void Create_preserves_metric_values_age_and_activity_level()
    {
        var measuredAt = new DateTimeOffset(2026, 9, 8, 12, 30, 0, TimeSpan.Zero);
        var input = new MeasurementInput(MeasurementType.WeightAndSizes, 80m, 180m, 40m, 90m, 35, ActivityLevel.Moderate, measuredAt);

        var result = Measurement.Create(ProfileId.New(), input, measuredAt);

        Assert.True(result.IsSuccess);
        Assert.Equal(80m, result.Value.WeightKg);
        Assert.Equal(180m, result.Value.HeightCm);
        Assert.Equal(40m, result.Value.NeckCm);
        Assert.Equal(90m, result.Value.AbdomenCm);
        Assert.Equal(35, result.Value.AgeYears);
        Assert.Equal(ActivityLevel.Moderate, result.Value.ActivityLevel);
        Assert.Equal(measuredAt, result.Value.MeasuredAtUtc);
    }

    [Theory]
    [InlineData(0, "measurement.weight.invalid")]
    [InlineData(-1, "measurement.weight.invalid")]
    public void Create_rejects_invalid_weight(decimal weightKg, string expectedCode)
    {
        var input = new MeasurementInput(MeasurementType.WeightAndSizes, weightKg, 180m, 40m, 90m, 35, ActivityLevel.Moderate, DateTimeOffset.UtcNow);

        var result = Measurement.Create(ProfileId.New(), input, DateTimeOffset.UtcNow);

        Assert.False(result.IsSuccess);
        Assert.Equal(expectedCode, result.Error!.Code);
    }

    [Fact]
    public void Create_rejects_invalid_age()
    {
        var input = new MeasurementInput(MeasurementType.WeightAndSizes, 80m, 180m, 40m, 90m, 0, ActivityLevel.Moderate, DateTimeOffset.UtcNow);

        var result = Measurement.Create(ProfileId.New(), input, DateTimeOffset.UtcNow);

        Assert.False(result.IsSuccess);
        Assert.Equal("measurement.age.invalid", result.Error!.Code);
    }

    [Theory]
    [InlineData(80, 0, 40, 90, "measurement.height.invalid")]
    [InlineData(80, 180, 0, 90, "measurement.neck.invalid")]
    [InlineData(80, 180, 40, 0, "measurement.abdomen.invalid")]
    public void Create_rejects_invalid_measurement_dimensions(decimal weightKg, decimal heightCm, decimal neckCm, decimal abdomenCm, string expectedCode)
    {
        var input = new MeasurementInput(MeasurementType.WeightAndSizes, weightKg, heightCm, neckCm, abdomenCm, 35, ActivityLevel.Moderate, DateTimeOffset.UtcNow);

        var result = Measurement.Create(ProfileId.New(), input, DateTimeOffset.UtcNow);

        Assert.False(result.IsSuccess);
        Assert.Equal(expectedCode, result.Error!.Code);
    }

    [Fact]
    public void Create_rejects_unknown_activity_level()
    {
        var input = new MeasurementInput(MeasurementType.WeightAndSizes, 80m, 180m, 40m, 90m, 35, (ActivityLevel)99, DateTimeOffset.UtcNow);

        var result = Measurement.Create(ProfileId.New(), input, DateTimeOffset.UtcNow);

        Assert.False(result.IsSuccess);
        Assert.Equal("measurement.activity.invalid", result.Error!.Code);
    }

    [Fact]
    public void Create_rejects_non_utc_timestamp()
    {
        var measuredAt = new DateTimeOffset(2026, 9, 8, 12, 30, 0, TimeSpan.FromHours(2));
        var input = new MeasurementInput(MeasurementType.WeightAndSizes, 80m, 180m, 40m, 90m, 35, ActivityLevel.Moderate, measuredAt);

        var result = Measurement.Create(ProfileId.New(), input, DateTimeOffset.UtcNow);

        Assert.False(result.IsSuccess);
        Assert.Equal("measurement.measuredAtUtc.invalid", result.Error!.Code);
    }

    [Fact]
    public void Create_weight_only_accepts_missing_sizes()
    {
        var input = new MeasurementInput(
            MeasurementType.WeightOnly,
            80m,
            180m,
            null,
            null,
            35,
            ActivityLevel.Moderate,
            DateTimeOffset.UtcNow);

        var result = Measurement.Create(ProfileId.New(), input, DateTimeOffset.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(MeasurementType.WeightOnly, result.Value.Type);
        Assert.Null(result.Value.NeckCm);
        Assert.Null(result.Value.AbdomenCm);
    }

    [Fact]
    public void Create_weight_only_rejects_supplied_sizes()
    {
        var input = new MeasurementInput(
            MeasurementType.WeightOnly,
            80m,
            180m,
            40m,
            90m,
            35,
            ActivityLevel.Moderate,
            DateTimeOffset.UtcNow);

        var result = Measurement.Create(ProfileId.New(), input, DateTimeOffset.UtcNow);

        Assert.False(result.IsSuccess);
        Assert.Equal("measurement.sizes.notAllowed", result.Error!.Code);
    }

    [Fact]
    public void Create_weight_and_sizes_requires_both_sizes()
    {
        var input = new MeasurementInput(
            MeasurementType.WeightAndSizes,
            80m,
            180m,
            40m,
            null,
            35,
            ActivityLevel.Moderate,
            DateTimeOffset.UtcNow);

        var result = Measurement.Create(ProfileId.New(), input, DateTimeOffset.UtcNow);

        Assert.False(result.IsSuccess);
        Assert.Equal("measurement.sizes.required", result.Error!.Code);
    }

    [Fact]
    public void Create_weight_and_sizes_validates_size_ranges()
    {
        var input = new MeasurementInput(
            MeasurementType.WeightAndSizes,
            80m,
            180m,
            0m,
            90m,
            35,
            ActivityLevel.Moderate,
            DateTimeOffset.UtcNow);

        var result = Measurement.Create(ProfileId.New(), input, DateTimeOffset.UtcNow);

        Assert.False(result.IsSuccess);
        Assert.Equal("measurement.neck.invalid", result.Error!.Code);
    }

    [Fact]
    public void Create_female_weight_and_sizes_requires_hip()
    {
        var input = new MeasurementInput(
            MeasurementType.WeightAndSizes,
            80m,
            180m,
            40m,
            90m,
            35,
            ActivityLevel.Moderate,
            DateTimeOffset.UtcNow,
            Gender: ProfileGender.Female);

        var result = Measurement.Create(ProfileId.New(), input, DateTimeOffset.UtcNow);

        Assert.False(result.IsSuccess);
        Assert.Equal("measurement.hip.required", result.Error!.Code);
    }

    [Fact]
    public void Create_preserves_female_gender_and_hip()
    {
        var input = new MeasurementInput(
            MeasurementType.WeightAndSizes,
            80m,
            180m,
            40m,
            90m,
            35,
            ActivityLevel.Moderate,
            DateTimeOffset.UtcNow,
            110.5m,
            ProfileGender.Female);

        var result = Measurement.Create(ProfileId.New(), input, DateTimeOffset.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(ProfileGender.Female, result.Value.Gender);
        Assert.Equal(110.5m, result.Value.HipCm);
    }

    [Fact]
    public void Create_rejects_unknown_measurement_type()
    {
        var input = new MeasurementInput(
            (MeasurementType)99,
            80m,
            180m,
            null,
            null,
            35,
            ActivityLevel.Moderate,
            DateTimeOffset.UtcNow);

        var result = Measurement.Create(ProfileId.New(), input, DateTimeOffset.UtcNow);

        Assert.False(result.IsSuccess);
        Assert.Equal("measurement.type.invalid", result.Error!.Code);
    }

    [Fact]
    public void Rehydrate_preserves_the_existing_id_when_editing_valid_values()
    {
        var profileId = ProfileId.New();
        var measurementId = MeasurementId.New();
        var measuredAt = new DateTimeOffset(2026, 9, 16, 12, 0, 0, TimeSpan.Zero);
        var input = new MeasurementInput(
            MeasurementType.WeightAndSizes,
            82.5m,
            181.5m,
            41.25m,
            91.75m,
            36,
            ActivityLevel.High,
            measuredAt);

        var result = Measurement.Rehydrate(measurementId, profileId, input);

        Assert.True(result.IsSuccess);
        Assert.Equal(measurementId, result.Value.Id);
        Assert.Equal(82.5m, result.Value.WeightKg);
        Assert.Equal(measuredAt, result.Value.MeasuredAtUtc);
    }

    [Fact]
    public void Rehydrate_rejects_an_invalid_female_edit_without_hip()
    {
        var input = new MeasurementInput(
            MeasurementType.WeightAndSizes,
            80m,
            180m,
            40m,
            90m,
            35,
            ActivityLevel.Moderate,
            DateTimeOffset.UtcNow,
            Gender: ProfileGender.Female);

        var result = Measurement.Rehydrate(MeasurementId.New(), ProfileId.New(), input);

        Assert.False(result.IsSuccess);
        Assert.Equal("measurement.hip.required", result.Error!.Code);
    }
}
