using Anthropometry.Application.Abstractions;
using Anthropometry.Application.Common;
using Anthropometry.Domain.Common;
using Anthropometry.Domain.Measurements;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.Application.Measurements;

public sealed class DeleteMeasurement
{
    private readonly IMeasurementRepository _measurements;

    public DeleteMeasurement(IMeasurementRepository measurements)
    {
        _measurements = measurements;
    }

    public async Task<Result> ExecuteAsync(
        ProfileId profileId,
        MeasurementId measurementId,
        CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _measurements.GetByIdAsync(measurementId, cancellationToken);
            if (existing is null || existing.ProfileId != profileId)
            {
                return Result.Failure(ApplicationErrors.MeasurementNotFound);
            }

            await _measurements.DeleteWithResultsAsync(measurementId, cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Result.Failure(ApplicationErrors.PersistenceUnavailable);
        }
    }
}
