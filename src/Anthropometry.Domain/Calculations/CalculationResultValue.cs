namespace Anthropometry.Domain.Calculations;

public sealed record CalculationResultValue(
    decimal Value,
    string Unit,
    string FormulaId,
    string FormulaVersion);
