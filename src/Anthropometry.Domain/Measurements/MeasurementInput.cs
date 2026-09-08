using Anthropometry.Domain.Calculations;

namespace Anthropometry.Domain.Measurements;

public sealed record MeasurementInput(
    decimal WeightKg,
    decimal HeightCm,
    decimal NeckCm,
    decimal AbdomenCm,
    int AgeYears,
    ActivityLevel ActivityLevel,
    DateTimeOffset MeasuredAtUtc);
