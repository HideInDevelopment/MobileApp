using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Measurements;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.Infrastructure.Tests.Support;

public static class TestData
{
    public static Profile Profile(string name = "Manuel")
        => Anthropometry.Domain.Profiles.Profile.Create(name, DateTimeOffset.UtcNow).Value;

    public static Measurement Measurement(ProfileId profileId, DateTimeOffset? measuredAtUtc = null)
    {
        var input = new MeasurementInput(
            80m,
            180m,
            40m,
            90m,
            35,
            ActivityLevel.Moderate,
            measuredAtUtc ?? DateTimeOffset.UtcNow);
        return Anthropometry.Domain.Measurements.Measurement.Create(profileId, input, DateTimeOffset.UtcNow).Value;
    }

    public static CalculationResult CalculationResult(MeasurementId measurementId, CalculationType type = CalculationType.BasalMetabolicRate)
        => Anthropometry.Domain.Calculations.CalculationResult.Create(
            measurementId,
            type,
            new CalculationResultValue(1755m, "kcal/day", "mifflin-st-jeor-male-bmr", "1.0"),
            DateTimeOffset.UtcNow).Value;
}
