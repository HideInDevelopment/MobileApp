using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Calculations.Bmr;
using Anthropometry.Domain.Calculations.BodyFat;
using Anthropometry.Domain.Calculations.Tdee;

namespace Anthropometry.Application.Abstractions;

public interface IFormulaCatalog
{
    ICalculationFormula<BodyFatInput, CalculationResultValue> MaleBodyFat { get; }

    ICalculationFormula<FemaleBodyFatInput, CalculationResultValue> FemaleBodyFat { get; }

    ICalculationFormula<BmrInput, CalculationResultValue> MaleBmr { get; }

    ICalculationFormula<BmrInput, CalculationResultValue> FemaleBmr { get; }

    ICalculationFormula<TdeeInput, CalculationResultValue> Tdee { get; }
}
