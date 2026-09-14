using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Measurements;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.Application.Common;

public sealed record ProfileSettingsDto(
    decimal HeightCm,
    int AgeYears,
    ActivityLevel ActivityLevel);

public sealed record ProfileDto(
    ProfileId Id,
    string Name,
    ProfileSettingsDto? Settings,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    ProfileGender Gender = ProfileGender.Male);

public sealed record MeasurementDto(
    MeasurementId Id,
    ProfileId ProfileId,
    MeasurementType Type,
    DateTimeOffset MeasuredAtUtc,
    decimal WeightKg,
    decimal HeightCm,
    decimal? NeckCm,
    decimal? AbdomenCm,
    int AgeYears,
    ActivityLevel ActivityLevel,
    decimal? HipCm = null,
    ProfileGender Gender = ProfileGender.Male);

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
        => new(
            profile.Id,
            profile.Name,
            profile.Settings is null
                ? null
                : new ProfileSettingsDto(profile.Settings.HeightCm, profile.Settings.AgeYears, profile.Settings.ActivityLevel),
            profile.CreatedAtUtc,
            profile.UpdatedAtUtc,
            profile.Gender);

    public static MeasurementDto ToDto(Measurement measurement)
        => new(measurement.Id, measurement.ProfileId, measurement.Type, measurement.MeasuredAtUtc, measurement.WeightKg, measurement.HeightCm, measurement.NeckCm, measurement.AbdomenCm, measurement.AgeYears, measurement.ActivityLevel, measurement.HipCm, measurement.Gender);

    public static CalculationResultDto ToDto(CalculationResult result)
        => new(result.Id, result.MeasurementId, result.CalculationType, result.FormulaId, result.FormulaVersion, result.Value, result.Unit, result.CalculatedAtUtc);
}
