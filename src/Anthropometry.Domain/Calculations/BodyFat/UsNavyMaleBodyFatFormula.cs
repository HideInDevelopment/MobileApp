using Anthropometry.Domain.Common;

namespace Anthropometry.Domain.Calculations.BodyFat;

public sealed class UsNavyMaleBodyFatFormula : ICalculationFormula<BodyFatInput, CalculationResultValue>
{
    private const double CentimetersPerInch = 2.54d;

    public CalculationType Type => CalculationType.BodyFatPercentage;

    public string FormulaId => "us-navy-male-body-fat";

    public string Version => "1.0";

    public Result<CalculationResultValue> Calculate(BodyFatInput input)
    {
        if (input is null || input.AbdomenCm <= 0m || input.NeckCm <= 0m || input.HeightCm <= 0m || input.AbdomenCm <= input.NeckCm)
        {
            return Result.Failure<CalculationResultValue>(
                new DomainError("calculation.bodyFat.input.invalid", "Errors.BodyFatInputInvalid"));
        }

        var abdomenInches = (double)input.AbdomenCm / CentimetersPerInch;
        var neckInches = (double)input.NeckCm / CentimetersPerInch;
        var heightInches = (double)input.HeightCm / CentimetersPerInch;
        var percentage =
            86.010d * Math.Log10(abdomenInches - neckInches)
            - 70.041d * Math.Log10(heightInches)
            + 36.76d;

        return Result.Success(new CalculationResultValue((decimal)percentage, "%", FormulaId, Version));
    }
}
