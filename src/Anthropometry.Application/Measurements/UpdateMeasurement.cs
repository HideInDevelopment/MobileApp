using Anthropometry.Application.Abstractions;
using Anthropometry.Application.Common;
using Anthropometry.Application.Entitlements;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Common;
using Anthropometry.Domain.Measurements;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.Application.Measurements;

public sealed record UpdateMeasurementCommand(
    MeasurementId MeasurementId,
    ProfileId ProfileId,
    MeasurementType Type,
    decimal WeightKg,
    decimal HeightCm,
    decimal? NeckCm,
    decimal? AbdomenCm,
    int AgeYears,
    ActivityLevel ActivityLevel,
    DateTimeOffset MeasuredAtUtc,
    decimal? HipCm = null,
    ProfileGender Gender = ProfileGender.Male);

public sealed class UpdateMeasurement
{
    private readonly IMeasurementRepository _measurements;
    private readonly ICalculationResultRepository _results;
    private readonly IClock? _clock;
    private readonly IEntitlementProvider _entitlementProvider;

    public UpdateMeasurement(
        IMeasurementRepository measurements,
        ICalculationResultRepository results,
        IClock? clock = null,
        IEntitlementProvider? entitlementProvider = null)
    {
        _measurements = measurements;
        _results = results;
        _clock = clock;
        _entitlementProvider = entitlementProvider ?? FreeEntitlementProvider.Instance;
    }

    public async Task<Result<MeasurementDto>> ExecuteAsync(
        UpdateMeasurementCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _measurements.GetByIdAsync(command.MeasurementId, cancellationToken);
            if (existing is null || existing.ProfileId != command.ProfileId)
            {
                return Result.Failure<MeasurementDto>(ApplicationErrors.MeasurementNotFound);
            }

            if (command.MeasuredAtUtc != existing.MeasuredAtUtc)
            {
                var entitlement = await _entitlementProvider.GetCurrentAsync(cancellationToken);
                var dateValidation = MeasurementDatePolicy.Validate(
                    command.MeasuredAtUtc,
                    entitlement,
                    _clock?.UtcNow ?? DateTimeOffset.UtcNow);
                if (!dateValidation.IsSuccess)
                {
                    return Result.Failure<MeasurementDto>(dateValidation.Error!);
                }
            }

            var updated = Measurement.Rehydrate(
                command.MeasurementId,
                command.ProfileId,
                new MeasurementInput(
                    command.Type,
                    command.WeightKg,
                    command.HeightCm,
                    command.NeckCm,
                    command.AbdomenCm,
                    command.AgeYears,
                    command.ActivityLevel,
                    command.MeasuredAtUtc,
                    command.HipCm,
                    command.Gender));
            if (!updated.IsSuccess)
            {
                return Result.Failure<MeasurementDto>(updated.Error!);
            }

            await _results.DeleteByMeasurementAsync(command.MeasurementId, cancellationToken);
            await _measurements.UpdateAsync(updated.Value, cancellationToken);
            return Result.Success(ApplicationModels.ToDto(updated.Value));
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
