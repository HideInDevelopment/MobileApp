using Anthropometry.Application.Abstractions;
using Anthropometry.Application.Common;
using Anthropometry.Application.Entitlements;
using Anthropometry.Domain.Common;
using Anthropometry.Domain.Measurements;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.Application.Measurements;

public sealed record RecordMeasurementCommand(
    ProfileId ProfileId,
    MeasurementType Type,
    decimal WeightKg,
    decimal? NeckCm,
    decimal? AbdomenCm,
    DateTimeOffset MeasuredAtUtc,
    decimal? HipCm = null);

public sealed class RecordMeasurement
{
    private readonly IProfileRepository _profiles;
    private readonly IMeasurementRepository _measurements;
    private readonly IClock _clock;
    private readonly IEntitlementProvider _entitlementProvider;

    public RecordMeasurement(
        IProfileRepository profiles,
        IMeasurementRepository measurements,
        IClock clock,
        IEntitlementProvider? entitlementProvider = null)
    {
        _profiles = profiles;
        _measurements = measurements;
        _clock = clock;
        _entitlementProvider = entitlementProvider ?? FreeEntitlementProvider.Instance;
    }

    public async Task<Result<MeasurementDto>> ExecuteAsync(RecordMeasurementCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var profile = await _profiles.GetByIdAsync(command.ProfileId, cancellationToken);
            if (profile is null)
            {
                return Result.Failure<MeasurementDto>(ApplicationErrors.ProfileNotFound);
            }

            if (profile.Settings is null)
            {
                return Result.Failure<MeasurementDto>(ApplicationErrors.ProfileSettingsRequired);
            }

            var entitlement = await _entitlementProvider.GetCurrentAsync(cancellationToken);
            var dateValidation = MeasurementDatePolicy.Validate(command.MeasuredAtUtc, entitlement, _clock.UtcNow);
            if (!dateValidation.IsSuccess)
            {
                return Result.Failure<MeasurementDto>(dateValidation.Error!);
            }

            var input = new MeasurementInput(
                command.Type,
                command.WeightKg,
                profile.Settings.HeightCm,
                command.NeckCm,
                command.AbdomenCm,
                profile.Settings.AgeYears,
                profile.Settings.ActivityLevel,
                command.MeasuredAtUtc,
                command.HipCm,
                profile.Gender);
            var measurement = Measurement.Create(command.ProfileId, input, _clock.UtcNow);
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
