using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Calculations.BodyFat;

namespace Anthropometry.Domain.Tests.Calculations.BodyFat;

public sealed class UsNavyFemaleBodyFatFormulaTests
{
    [Fact]
    public void Calculate_converts_metric_measurements_and_returns_versioned_percentage()
    {
        var formula = new UsNavyFemaleBodyFatFormula();

        var result = formula.Calculate(new FemaleBodyFatInput(106.68m, 111.76m, 38.1m, 162.56m));

        Assert.True(result.IsSuccess);
        Assert.Equal(CalculationType.BodyFatPercentage, formula.Type);
        Assert.Equal("us-navy-female-body-fat", result.Value.FormulaId);
        Assert.Equal("1.0", result.Value.FormulaVersion);
        Assert.Equal("%", result.Value.Unit);
        Assert.InRange(result.Value.Value, 47.3m, 47.4m);
    }

    [Theory]
    [InlineData(0, 111.76, 38.1, 162.56)]
    [InlineData(106.68, 0, 38.1, 162.56)]
    [InlineData(106.68, 111.76, 0, 162.56)]
    [InlineData(106.68, 111.76, 38.1, 0)]
    [InlineData(106.68, 111.76, 220, 162.56)]
    public void Calculate_rejects_invalid_logarithm_inputs(
        decimal waistCm,
        decimal hipCm,
        decimal neckCm,
        decimal heightCm)
    {
        var formula = new UsNavyFemaleBodyFatFormula();

        var result = formula.Calculate(new FemaleBodyFatInput(waistCm, hipCm, neckCm, heightCm));

        Assert.False(result.IsSuccess);
        Assert.Equal("calculation.bodyFat.input.invalid", result.Error!.Code);
    }
}
