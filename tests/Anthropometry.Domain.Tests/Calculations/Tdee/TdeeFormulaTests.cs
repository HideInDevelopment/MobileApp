using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Calculations.Tdee;

namespace Anthropometry.Domain.Tests.Calculations.Tdee;

public sealed class TdeeFormulaTests
{
    [Theory]
    [InlineData(ActivityLevel.Sedentary, 1.2)]
    [InlineData(ActivityLevel.Light, 1.375)]
    [InlineData(ActivityLevel.Moderate, 1.55)]
    [InlineData(ActivityLevel.High, 1.725)]
    [InlineData(ActivityLevel.VeryHigh, 1.9)]
    public void Calculate_multiplies_bmr_by_the_named_activity_factor(ActivityLevel level, decimal factor)
    {
        var formula = new TdeeFormula();

        var result = formula.Calculate(new TdeeInput(1755m, level));

        Assert.True(result.IsSuccess);
        Assert.Equal(1755m * factor, result.Value.Value);
        Assert.Equal("kcal/day", result.Value.Unit);
        Assert.Equal("tdee-activity-multiplier", result.Value.FormulaId);
        Assert.Equal("1.0", result.Value.FormulaVersion);
    }

    [Fact]
    public void Calculate_rejects_unknown_activity_level()
    {
        var formula = new TdeeFormula();

        var result = formula.Calculate(new TdeeInput(1755m, ActivityLevel.Unknown));

        Assert.False(result.IsSuccess);
        Assert.Equal("calculation.tdee.activity.invalid", result.Error!.Code);
    }
}
