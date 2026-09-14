using Anthropometry.Domain.Common;

namespace Anthropometry.Domain.Calculations.Bmr;

public sealed class MifflinStJeorFemaleBmrFormula : ICalculationFormula<BmrInput, CalculationResultValue>
{
    public CalculationType Type => CalculationType.BasalMetabolicRate;

    public string FormulaId => "mifflin-st-jeor-female-bmr";

    public string Version => "1.0";

    public Result<CalculationResultValue> Calculate(BmrInput input)
    {
        if (input is null || input.WeightKg <= 0m || input.HeightCm <= 0m || input.AgeYears <= 0)
        {
            return Result.Failure<CalculationResultValue>(
                new DomainError("calculation.bmr.input.invalid", "Errors.BmrInputInvalid"));
        }

        var bmr = 10m * input.WeightKg + 6.25m * input.HeightCm - 5m * input.AgeYears - 161m;
        return Result.Success(new CalculationResultValue(bmr, "kcal/day", FormulaId, Version));
    }
}
