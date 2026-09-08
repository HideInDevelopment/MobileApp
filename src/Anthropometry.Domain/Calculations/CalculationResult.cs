using Anthropometry.Domain.Common;
using Anthropometry.Domain.Measurements;

namespace Anthropometry.Domain.Calculations;

public sealed class CalculationResult
{
    private CalculationResult(
        CalculationResultId id,
        MeasurementId measurementId,
        CalculationType calculationType,
        CalculationResultValue value,
        DateTimeOffset calculatedAtUtc)
    {
        Id = id;
        MeasurementId = measurementId;
        CalculationType = calculationType;
        FormulaId = value.FormulaId;
        FormulaVersion = value.FormulaVersion;
        Value = value.Value;
        Unit = value.Unit;
        CalculatedAtUtc = calculatedAtUtc;
    }

    public CalculationResultId Id { get; }

    public MeasurementId MeasurementId { get; }

    public CalculationType CalculationType { get; }

    public string FormulaId { get; }

    public string FormulaVersion { get; }

    public decimal Value { get; }

    public string Unit { get; }

    public DateTimeOffset CalculatedAtUtc { get; }

    public static Result<CalculationResult> Create(
        MeasurementId measurementId,
        CalculationType calculationType,
        CalculationResultValue value,
        DateTimeOffset calculatedAtUtc)
    {
        if (measurementId.Value == Guid.Empty)
        {
            return Result.Failure<CalculationResult>(new DomainError("calculation.measurementId.invalid", "Errors.CalculationMeasurementIdInvalid"));
        }

        if (!Enum.IsDefined(calculationType))
        {
            return Result.Failure<CalculationResult>(new DomainError("calculation.type.invalid", "Errors.CalculationTypeInvalid"));
        }

        var valueError = ValidateValue(value);
        if (valueError is not null)
        {
            return Result.Failure<CalculationResult>(valueError);
        }

        var timestampError = Guard.Utc(calculatedAtUtc, "calculation.calculatedAtUtc.invalid", "Errors.CalculationCalculatedAtUtcInvalid");
        return timestampError is null
            ? Result.Success(new CalculationResult(CalculationResultId.New(), measurementId, calculationType, value, calculatedAtUtc))
            : Result.Failure<CalculationResult>(timestampError);
    }

    public static Result<CalculationResult> Rehydrate(
        CalculationResultId id,
        MeasurementId measurementId,
        CalculationType calculationType,
        CalculationResultValue value,
        DateTimeOffset calculatedAtUtc)
    {
        if (id.Value == Guid.Empty)
        {
            return Result.Failure<CalculationResult>(new DomainError("calculation.id.invalid", "Errors.CalculationIdInvalid"));
        }

        var created = Create(measurementId, calculationType, value, calculatedAtUtc);
        return created.IsSuccess
            ? Result.Success(new CalculationResult(id, measurementId, calculationType, value, calculatedAtUtc))
            : created;
    }

    private static DomainError? ValidateValue(CalculationResultValue? value)
    {
        if (value is null)
        {
            return new DomainError("calculation.value.required", "Errors.CalculationValueRequired");
        }

        if (string.IsNullOrWhiteSpace(value.FormulaId) || string.IsNullOrWhiteSpace(value.FormulaVersion) || string.IsNullOrWhiteSpace(value.Unit))
        {
            return new DomainError("calculation.formula.invalid", "Errors.CalculationFormulaInvalid");
        }

        return null;
    }
}
