using Anthropometry.Application.Abstractions;
using Anthropometry.Application.Common;
using Anthropometry.Domain.Common;
using Anthropometry.Domain.Measurements;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.Application.Measurements;

public sealed record RecordMeasurementCommand(ProfileId ProfileId, MeasurementInput Input);

public sealed class RecordMeasurement
{
    private readonly IProfileRepository _profiles;
    private readonly IMeasurementRepository _measurements;
    private readonly IClock _clock;

    public RecordMeasurement(IProfileRepository profiles, IMeasurementRepository measurements, IClock clock)
    {
        _profiles = profiles;
        _measurements = measurements;
        _clock = clock;
    }

    public async Task<Result<MeasurementDto>> ExecuteAsync(RecordMeasurementCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (await _profiles.GetByIdAsync(command.ProfileId, cancellationToken) is null)
            {
                return Result.Failure<MeasurementDto>(ApplicationErrors.ProfileNotFound);
            }

            var measurement = Measurement.Create(command.ProfileId, command.Input, _clock.UtcNow);
            if (!measurement.IsSuccess)
            {
                return Result.Failure<MeasurementDto>(measurement.Error!);
            }

            await _measurements.AddAsync(measurement.Value, cancellationToken);
            return Result.Success(ApplicationModels.ToDto(measurement.Value));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Result.Failure<MeasurementDto>(ApplicationErrors.PersistenceUnavailable);
        }
    }
}
