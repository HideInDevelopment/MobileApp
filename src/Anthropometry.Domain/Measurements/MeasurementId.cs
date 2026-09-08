namespace Anthropometry.Domain.Measurements;

public readonly record struct MeasurementId(Guid Value)
{
    public static MeasurementId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString("D");
}
