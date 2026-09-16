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

    public Task<Result<IReadOnlyList<MeasurementDto>>> ExecuteAsync(ProfileId profileId, CancellationToken cancellationToken)
        => ExecuteAsync(new MeasurementHistoryQuery(profileId, null, null, null), cancellationToken);

    public async Task<Result<IReadOnlyList<MeasurementDto>>> ExecuteAsync(MeasurementHistoryQuery query, CancellationToken cancellationToken)
    {
        try
        {
            if (query.FromUtc.HasValue && query.ToUtc.HasValue && query.FromUtc > query.ToUtc)
            {
                return Result.Failure<IReadOnlyList<MeasurementDto>>(ApplicationErrors.MeasurementHistoryDateRangeInvalid);
            }

            var measurements = await _repository.GetByProfileAsync(query.ProfileId, cancellationToken);
            return Result.Success<IReadOnlyList<MeasurementDto>>(
                measurements
                    .Where(measurement => !query.FromUtc.HasValue || measurement.MeasuredAtUtc >= query.FromUtc.Value)
                    .Where(measurement => !query.ToUtc.HasValue || measurement.MeasuredAtUtc <= query.ToUtc.Value)
                    .Where(measurement => !query.Type.HasValue || measurement.Type == query.Type.Value)
                    .OrderByDescending(measurement => measurement.MeasuredAtUtc)
                    .Select(ApplicationModels.ToDto).ToArray());
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
