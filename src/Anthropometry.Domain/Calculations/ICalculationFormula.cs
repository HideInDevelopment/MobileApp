using Anthropometry.Domain.Common;

namespace Anthropometry.Domain.Calculations;

public interface ICalculationFormula<in TInput, TResult>
{
    CalculationType Type { get; }

    string FormulaId { get; }

    string Version { get; }

    Result<TResult> Calculate(TInput input);
}
