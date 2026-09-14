using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.Domain.Measurements;

public sealed record MeasurementInput(
    MeasurementType Type,
    decimal WeightKg,
    decimal HeightCm,
    decimal? NeckCm,
    decimal? AbdomenCm,
    int AgeYears,
    ActivityLevel ActivityLevel,
    DateTimeOffset MeasuredAtUtc,
    decimal? HipCm = null,
    ProfileGender Gender = ProfileGender.Male);
