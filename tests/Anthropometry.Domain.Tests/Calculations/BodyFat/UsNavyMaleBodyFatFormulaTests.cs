using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Calculations.BodyFat;

namespace Anthropometry.Domain.Tests.Calculations.BodyFat;

public sealed class UsNavyMaleBodyFatFormulaTests
{
    [Fact]
    public void Calculate_converts_centimeters_to_inches_and_returns_versioned_percentage()
    {
        var formula = new UsNavyMaleBodyFatFormula();
        var input = new BodyFatInput(90m, 40m, 180m);

        var result = formula.Calculate(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(CalculationType.BodyFatPercentage, formula.Type);
        Assert.Equal("us-navy-male-body-fat", result.Value.FormulaId);
        Assert.Equal("1.0", result.Value.FormulaVersion);
        Assert.Equal("%", result.Value.Unit);
        Assert.InRange(result.Value.Value, 18.4m, 18.5m);
    }

    [Theory]
    [InlineData(0, 40, 180)]
    [InlineData(90, 0, 180)]
    [InlineData(90, 40, 0)]
    [InlineData(40, 40, 180)]
    public void Calculate_rejects_invalid_logarithm_inputs(decimal abdomenCm, decimal neckCm, decimal heightCm)
    {
        var formula = new UsNavyMaleBodyFatFormula();

        var result = formula.Calculate(new BodyFatInput(abdomenCm, neckCm, heightCm));

        Assert.False(result.IsSuccess);
        Assert.Equal("calculation.bodyFat.input.invalid", result.Error!.Code);
    }
}
