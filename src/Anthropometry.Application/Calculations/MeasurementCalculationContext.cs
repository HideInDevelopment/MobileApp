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
        if (measurement.Type == MeasurementType.WeightAndSizes
            && measurement.NeckCm.HasValue
            && measurement.AbdomenCm.HasValue)
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
                && candidate.NeckCm.HasValue
                && candidate.AbdomenCm.HasValue)
            .OrderByDescending(candidate => candidate.MeasuredAtUtc)
            .FirstOrDefault();
    }
}
