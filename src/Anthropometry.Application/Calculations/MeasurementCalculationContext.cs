using Anthropometry.Application.Abstractions;
using Anthropometry.Domain.Measurements;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.Application.Calculations;

internal static class MeasurementCalculationContext
{
    public static async Task<Measurement?> GetSizeSourceAsync(
        IMeasurementRepository measurements,
        ProfileId profileId,
        Measurement measurement,
        CancellationToken cancellationToken)
    {
        if (HasRequiredSizes(measurement))
        {
            return measurement;
        }

        if (measurement.Type != MeasurementType.WeightOnly)
        {
            return null;
        }

        var history = await measurements.GetByProfileAsync(profileId, cancellationToken);
        return history
            .Where(candidate => candidate.Type == MeasurementType.WeightAndSizes
                && candidate.MeasuredAtUtc < measurement.MeasuredAtUtc
                && candidate.Gender == measurement.Gender
                && HasRequiredSizes(candidate))
            .OrderByDescending(candidate => candidate.MeasuredAtUtc)
            .FirstOrDefault();
    }

    private static bool HasRequiredSizes(Measurement measurement)
        => measurement.Type == MeasurementType.WeightAndSizes
            && measurement.NeckCm.HasValue
            && measurement.AbdomenCm.HasValue
            && (measurement.Gender != ProfileGender.Female || measurement.HipCm.HasValue);
}
