using Anthropometry.Application.Abstractions;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Calculations.Bmr;
using Anthropometry.Domain.Calculations.BodyFat;
using Anthropometry.Domain.Calculations.Tdee;

namespace Anthropometry.Application.Calculations;

public sealed class FormulaCatalog : IFormulaCatalog
{
    public FormulaCatalog(
        ICalculationFormula<BodyFatInput, CalculationResultValue> bodyFat,
        ICalculationFormula<BmrInput, CalculationResultValue> bmr,
        ICalculationFormula<TdeeInput, CalculationResultValue> tdee)
    {
        var identities = new[] { bodyFat.FormulaId, bmr.FormulaId, tdee.FormulaId };
        if (identities.Any(string.IsNullOrWhiteSpace) || identities.Distinct(StringComparer.Ordinal).Count() != identities.Length)
        {
            throw new ArgumentException("Formula identities must be unique and non-empty.");
        }

        BodyFat = bodyFat;
        Bmr = bmr;
        Tdee = tdee;
    }

    public ICalculationFormula<BodyFatInput, CalculationResultValue> BodyFat { get; }

    public ICalculationFormula<BmrInput, CalculationResultValue> Bmr { get; }

    public ICalculationFormula<TdeeInput, CalculationResultValue> Tdee { get; }
}
