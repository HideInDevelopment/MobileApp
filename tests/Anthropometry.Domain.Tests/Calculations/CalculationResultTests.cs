using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Common;
using Anthropometry.Domain.Measurements;

namespace Anthropometry.Domain.Tests.Calculations;

public sealed class CalculationResultTests
{
    [Fact]
    public void Create_preserves_formula_identity_version_and_unit()
    {
        var calculatedAt = DateTimeOffset.UtcNow;
        var measurementId = MeasurementId.New();
        var value = new CalculationResultValue(18.46m, "%", "us-navy-male-body-fat", "1.0");

        var result = CalculationResult.Create(measurementId, CalculationType.BodyFatPercentage, value, calculatedAt);

        Assert.True(result.IsSuccess);
        Assert.Equal(measurementId, result.Value.MeasurementId);
        Assert.Equal(CalculationType.BodyFatPercentage, result.Value.CalculationType);
        Assert.Equal("us-navy-male-body-fat", result.Value.FormulaId);
        Assert.Equal("1.0", result.Value.FormulaVersion);
        Assert.Equal("%", result.Value.Unit);
        Assert.Equal(calculatedAt, result.Value.CalculatedAtUtc);
    }

    [Fact]
    public void Create_rejects_empty_formula_identity()
    {
        var value = new CalculationResultValue(18.46m, "%", "", "1.0");

        var result = CalculationResult.Create(MeasurementId.New(), CalculationType.BodyFatPercentage, value, DateTimeOffset.UtcNow);

        Assert.False(result.IsSuccess);
        Assert.Equal("calculation.formula.invalid", result.Error!.Code);
    }
}
