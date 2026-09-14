using Anthropometry.Domain.Common;

namespace Anthropometry.Domain.Calculations.BodyFat;

public sealed class UsNavyFemaleBodyFatFormula : ICalculationFormula<FemaleBodyFatInput, CalculationResultValue>
{
    private const double CentimetersPerInch = 2.54d;

    public CalculationType Type => CalculationType.BodyFatPercentage;

    public string FormulaId => "us-navy-female-body-fat";

    public string Version => "1.0";

    public Result<CalculationResultValue> Calculate(FemaleBodyFatInput input)
    {
        if (input is null
            || input.WaistCm <= 0m
            || input.HipCm <= 0m
            || input.NeckCm <= 0m
            || input.HeightCm <= 0m
            || input.WaistCm + input.HipCm <= input.NeckCm)
        {
            return Result.Failure<CalculationResultValue>(
                new DomainError("calculation.bodyFat.input.invalid", "Errors.BodyFatInputInvalid"));
        }

        var waistInches = (double)input.WaistCm / CentimetersPerInch;
        var hipInches = (double)input.HipCm / CentimetersPerInch;
        var neckInches = (double)input.NeckCm / CentimetersPerInch;
        var heightInches = (double)input.HeightCm / CentimetersPerInch;
        var percentage =
            163.205d * Math.Log10(waistInches + hipInches - neckInches)
            - 97.684d * Math.Log10(heightInches)
            - 78.387d;

        return Result.Success(new CalculationResultValue((decimal)percentage, "%", FormulaId, Version));
    }
}
