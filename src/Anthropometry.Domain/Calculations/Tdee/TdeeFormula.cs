using Anthropometry.Domain.Common;

namespace Anthropometry.Domain.Calculations.Tdee;

public sealed class TdeeFormula : ICalculationFormula<TdeeInput, CalculationResultValue>
{
    public CalculationType Type => CalculationType.TotalDailyEnergyExpenditure;

    public string FormulaId => "tdee-activity-multiplier";

    public string Version => "1.0";

    public Result<CalculationResultValue> Calculate(TdeeInput input)
    {
        if (input is null || input.BmrKcalPerDay <= 0m)
        {
            return Result.Failure<CalculationResultValue>(
                new DomainError("calculation.tdee.input.invalid", "Errors.TdeeInputInvalid"));
        }

        if (!ActivityFactorTable.TryGet(input.ActivityLevel, out var definition))
        {
            return Result.Failure<CalculationResultValue>(
                new DomainError("calculation.tdee.activity.invalid", "Errors.TdeeActivityInvalid"));
        }

        var tdee = input.BmrKcalPerDay * definition!.Factor;
        return Result.Success(new CalculationResultValue(tdee, "kcal/day", FormulaId, Version));
    }
}
