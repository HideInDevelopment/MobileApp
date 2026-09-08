using Anthropometry.Application.Abstractions;
using Anthropometry.Application.Common;
using Anthropometry.Domain.Common;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.Application.Measurements;

public sealed class GetMeasurementHistory
{
    private readonly IMeasurementRepository _repository;

    public GetMeasurementHistory(IMeasurementRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<MeasurementDto>>> ExecuteAsync(ProfileId profileId, CancellationToken cancellationToken)
    {
        try
        {
            var measurements = await _repository.GetByProfileAsync(profileId, cancellationToken);
            return Result.Success<IReadOnlyList<MeasurementDto>>(measurements.OrderByDescending(measurement => measurement.MeasuredAtUtc).Select(ApplicationModels.ToDto).ToArray());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Result.Failure<IReadOnlyList<MeasurementDto>>(ApplicationErrors.PersistenceUnavailable);
        }
    }
}
