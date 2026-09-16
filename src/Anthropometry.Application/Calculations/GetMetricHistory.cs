using Anthropometry.Application.Abstractions;
using Anthropometry.Application.Common;
using Anthropometry.Domain.Common;

namespace Anthropometry.Application.Calculations;

public sealed class GetMetricHistory
{
    private readonly IMeasurementRepository _measurements;
    private readonly ICalculationResultRepository _results;

    public GetMetricHistory(
        IMeasurementRepository measurements,
        ICalculationResultRepository results)
    {
        _measurements = measurements;
        _results = results;
    }

    public async Task<Result<IReadOnlyList<MetricHistoryDto>>> ExecuteAsync(
        MetricHistoryQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            if (query.FromUtc.HasValue && query.ToUtc.HasValue && query.FromUtc > query.ToUtc)
            {
                return Result.Failure<IReadOnlyList<MetricHistoryDto>>(ApplicationErrors.MetricHistoryDateRangeInvalid);
            }

            var measurements = (await _measurements.GetByProfileAsync(query.ProfileId, cancellationToken))
                .Where(measurement => !query.FromUtc.HasValue || measurement.MeasuredAtUtc >= query.FromUtc.Value)
                .Where(measurement => !query.ToUtc.HasValue || measurement.MeasuredAtUtc <= query.ToUtc.Value)
                .ToArray();

            if (!query.CalculationType.HasValue)
            {
                return Result.Success<IReadOnlyList<MetricHistoryDto>>(
                    measurements
                        .OrderBy(measurement => measurement.MeasuredAtUtc)
                        .Select(measurement => new MetricHistoryDto(
                            measurement.Id,
                            measurement.MeasuredAtUtc,
                            null,
                            measurement.WeightKg,
                            "kg"))
                        .ToArray());
            }

            var history = new List<MetricHistoryDto>();
            foreach (var measurement in measurements)
            {
                var result = (await _results.GetByMeasurementAsync(measurement.Id, cancellationToken))
                    .Where(candidate => candidate.CalculationType == query.CalculationType.Value)
                    .OrderByDescending(candidate => candidate.CalculatedAtUtc)
                    .FirstOrDefault();
                if (result is null)
                {
                    continue;
                }

                history.Add(new MetricHistoryDto(
                    measurement.Id,
                    measurement.MeasuredAtUtc,
                    result.CalculationType,
                    result.Value,
                    result.Unit));
            }

            return Result.Success<IReadOnlyList<MetricHistoryDto>>(
                history.OrderBy(point => point.MeasuredAtUtc).ToArray());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Result.Failure<IReadOnlyList<MetricHistoryDto>>(ApplicationErrors.PersistenceUnavailable);
        }
    }
}
