namespace Anthropometry.Domain.Calculations;

public readonly record struct CalculationResultId(Guid Value)
{
    public static CalculationResultId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString("D");
}
