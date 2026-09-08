using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Measurements;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.Application.Common;

public sealed record ProfileDto(
    ProfileId Id,
    string Name,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record MeasurementDto(
    MeasurementId Id,
    ProfileId ProfileId,
    DateTimeOffset MeasuredAtUtc,
    decimal WeightKg,
    decimal HeightCm,
    decimal NeckCm,
    decimal AbdomenCm,
    int AgeYears,
    ActivityLevel ActivityLevel);

public sealed record CalculationResultDto(
    CalculationResultId Id,
    MeasurementId MeasurementId,
    CalculationType CalculationType,
    string FormulaId,
    string FormulaVersion,
    decimal Value,
    string Unit,
    DateTimeOffset CalculatedAtUtc);

internal static class ApplicationModels
{
    public static ProfileDto ToDto(Profile profile)
        => new(profile.Id, profile.Name, profile.CreatedAtUtc, profile.UpdatedAtUtc);

    public static MeasurementDto ToDto(Measurement measurement)
        => new(measurement.Id, measurement.ProfileId, measurement.MeasuredAtUtc, measurement.WeightKg, measurement.HeightCm, measurement.NeckCm, measurement.AbdomenCm, measurement.AgeYears, measurement.ActivityLevel);

    public static CalculationResultDto ToDto(CalculationResult result)
        => new(result.Id, result.MeasurementId, result.CalculationType, result.FormulaId, result.FormulaVersion, result.Value, result.Unit, result.CalculatedAtUtc);
}
