using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Calculations.Bmr;
using Anthropometry.Domain.Calculations.BodyFat;
using Anthropometry.Domain.Calculations.Tdee;

namespace Anthropometry.Application.Abstractions;

public interface IFormulaCatalog
{
    ICalculationFormula<BodyFatInput, CalculationResultValue> BodyFat { get; }

    ICalculationFormula<BmrInput, CalculationResultValue> Bmr { get; }

    ICalculationFormula<TdeeInput, CalculationResultValue> Tdee { get; }
}
