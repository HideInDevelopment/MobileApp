namespace Anthropometry.Domain.Calculations.BodyFat;

public sealed record FemaleBodyFatInput(
    decimal WaistCm,
    decimal HipCm,
    decimal NeckCm,
    decimal HeightCm);
