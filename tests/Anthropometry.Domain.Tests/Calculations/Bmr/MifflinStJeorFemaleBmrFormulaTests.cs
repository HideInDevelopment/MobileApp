using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Calculations.Bmr;

namespace Anthropometry.Domain.Tests.Calculations.Bmr;

public sealed class MifflinStJeorFemaleBmrFormulaTests
{
    [Fact]
    public void Calculate_returns_female_mifflin_st_jeor_kcal_per_day()
    {
        var formula = new MifflinStJeorFemaleBmrFormula();

        var result = formula.Calculate(new BmrInput(80m, 180m, 35));

        Assert.True(result.IsSuccess);
        Assert.Equal(1589m, result.Value.Value);
        Assert.Equal("kcal/day", result.Value.Unit);
        Assert.Equal("mifflin-st-jeor-female-bmr", result.Value.FormulaId);
        Assert.Equal("1.0", result.Value.FormulaVersion);
    }

    [Fact]
    public void Calculate_rejects_non_positive_input()
    {
        var formula = new MifflinStJeorFemaleBmrFormula();

        var result = formula.Calculate(new BmrInput(0m, 180m, 35));

        Assert.False(result.IsSuccess);
        Assert.Equal("calculation.bmr.input.invalid", result.Error!.Code);
    }
}
