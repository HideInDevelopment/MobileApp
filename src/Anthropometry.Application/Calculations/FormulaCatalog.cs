using Anthropometry.Application.Abstractions;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Calculations.Bmr;
using Anthropometry.Domain.Calculations.BodyFat;
using Anthropometry.Domain.Calculations.Tdee;

namespace Anthropometry.Application.Calculations;

public sealed class FormulaCatalog : IFormulaCatalog
{
    public FormulaCatalog(
        ICalculationFormula<BodyFatInput, CalculationResultValue> maleBodyFat,
        ICalculationFormula<BmrInput, CalculationResultValue> maleBmr,
        ICalculationFormula<TdeeInput, CalculationResultValue> tdee,
        ICalculationFormula<FemaleBodyFatInput, CalculationResultValue> femaleBodyFat,
        ICalculationFormula<BmrInput, CalculationResultValue> femaleBmr)
    {
        var identities = new[]
        {
            maleBodyFat.FormulaId,
            femaleBodyFat.FormulaId,
            maleBmr.FormulaId,
            femaleBmr.FormulaId,
            tdee.FormulaId
        };
        if (identities.Any(string.IsNullOrWhiteSpace) || identities.Distinct(StringComparer.Ordinal).Count() != identities.Length)
        {
            throw new ArgumentException("Formula identities must be unique and non-empty.");
        }

        MaleBodyFat = maleBodyFat;
        FemaleBodyFat = femaleBodyFat;
        MaleBmr = maleBmr;
        FemaleBmr = femaleBmr;
        Tdee = tdee;
    }

    public ICalculationFormula<BodyFatInput, CalculationResultValue> MaleBodyFat { get; }

    public ICalculationFormula<FemaleBodyFatInput, CalculationResultValue> FemaleBodyFat { get; }

    public ICalculationFormula<BmrInput, CalculationResultValue> MaleBmr { get; }

    public ICalculationFormula<BmrInput, CalculationResultValue> FemaleBmr { get; }

    public ICalculationFormula<TdeeInput, CalculationResultValue> Tdee { get; }
}
