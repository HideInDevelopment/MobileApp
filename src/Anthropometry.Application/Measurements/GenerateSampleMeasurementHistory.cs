using Anthropometry.Application.Abstractions;
using Anthropometry.Application.Calculations;
using Anthropometry.Application.Common;
using Anthropometry.Domain.Common;
using Anthropometry.Domain.Measurements;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.Application.Measurements;

public sealed record GenerateSampleMeasurementHistoryCommand(ProfileId ProfileId);

public sealed class GenerateSampleMeasurementHistory
{
    private const int SampleDayCount = 30;

    private readonly IProfileRepository _profiles;
    private readonly IMeasurementRepository _measurements;
    private readonly CalculateBodyFat _calculateBodyFat;
    private readonly CalculateBasalMetabolicRate _calculateBmr;
    private readonly CalculateTotalDailyEnergyExpenditure _calculateTdee;
    private readonly IClock _clock;

    public GenerateSampleMeasurementHistory(
        IProfileRepository profiles,
        IMeasurementRepository measurements,
        CalculateBodyFat calculateBodyFat,
        CalculateBasalMetabolicRate calculateBmr,
        CalculateTotalDailyEnergyExpenditure calculateTdee,
        IClock clock)
    {
        _profiles = profiles;
        _measurements = measurements;
        _calculateBodyFat = calculateBodyFat;
        _calculateBmr = calculateBmr;
        _calculateTdee = calculateTdee;
        _clock = clock;
    }

    public async Task<Result<int>> ExecuteAsync(
        GenerateSampleMeasurementHistoryCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var profile = await _profiles.GetByIdAsync(command.ProfileId, cancellationToken);
            if (profile is null)
            {
                return Result.Failure<int>(ApplicationErrors.ProfileNotFound);
            }

            if (profile.Settings is null)
            {
                return Result.Failure<int>(ApplicationErrors.ProfileSettingsRequired);
            }

            var history = await _measurements.GetByProfileAsync(command.ProfileId, cancellationToken);
            var sizeSource = history
                .Where(measurement => measurement.Type == MeasurementType.WeightAndSizes
                    && measurement.Gender == profile.Gender
                    && measurement.NeckCm.HasValue
                    && measurement.AbdomenCm.HasValue
                    && (profile.Gender != ProfileGender.Female || measurement.HipCm.HasValue))
                .OrderByDescending(measurement => measurement.MeasuredAtUtc)
                .FirstOrDefault();

            if (sizeSource is null)
            {
                return Result.Failure<int>(ApplicationErrors.SampleDataRequiresSizeMeasurement);
            }

            var todayUtc = _clock.UtcNow.UtcDateTime.Date;
            var existingDates = history
                .Where(measurement => measurement.MeasuredAtUtc.Date >= todayUtc.AddDays(-(SampleDayCount - 1))
                    && measurement.MeasuredAtUtc.Date <= todayUtc)
                .Select(measurement => (measurement.MeasuredAtUtc, measurement.Type))
                .ToHashSet();
            var generatedCount = 0;

            for (var dayIndex = 0; dayIndex < SampleDayCount; dayIndex++)
            {
                var type = dayIndex % 2 == 0
                    ? MeasurementType.WeightAndSizes
                    : MeasurementType.WeightOnly;
                var measuredAtUtc = new DateTimeOffset(
                    todayUtc.AddDays(dayIndex - (SampleDayCount - 1)).AddHours(12),
                    TimeSpan.Zero);

                if (existingDates.Contains((measuredAtUtc, type)))
                {
                    continue;
                }

                var input = new MeasurementInput(
                    type,
                    Math.Clamp(sizeSource.WeightKg + WeightVariation(dayIndex), 1m, 500m),
                    sizeSource.HeightCm,
                    type == MeasurementType.WeightAndSizes
                        ? Math.Clamp(sizeSource.NeckCm!.Value + SizeVariation(dayIndex), 1m, 100m)
                        : null,
                    type == MeasurementType.WeightAndSizes
                        ? Math.Clamp(sizeSource.AbdomenCm!.Value + AbdomenVariation(dayIndex), 1m, 400m)
                        : null,
                    sizeSource.AgeYears,
                    sizeSource.ActivityLevel,
                    measuredAtUtc,
                    type == MeasurementType.WeightAndSizes && profile.Gender == ProfileGender.Female
                        ? Math.Clamp(sizeSource.HipCm!.Value + HipVariation(dayIndex), 1m, 400m)
                        : null,
                    profile.Gender);
                var created = Measurement.Create(command.ProfileId, input, _clock.UtcNow);
                if (!created.IsSuccess)
                {
                    return Result.Failure<int>(created.Error!);
                }

                await _measurements.AddAsync(created.Value, cancellationToken);

                var measurementCommand = created.Value.Id;
                var bodyFat = await _calculateBodyFat.ExecuteAsync(
                    new CalculateBodyFatCommand(command.ProfileId, measurementCommand),
                    cancellationToken);
                if (!bodyFat.IsSuccess)
                {
                    return Result.Failure<int>(bodyFat.Error!);
                }

                var bmr = await _calculateBmr.ExecuteAsync(
                    new CalculateBmrCommand(command.ProfileId, measurementCommand),
                    cancellationToken);
                if (!bmr.IsSuccess)
                {
                    return Result.Failure<int>(bmr.Error!);
                }

                var tdee = await _calculateTdee.ExecuteAsync(
                    new CalculateTdeeCommand(command.ProfileId, measurementCommand),
                    cancellationToken);
                if (!tdee.IsSuccess)
                {
                    return Result.Failure<int>(tdee.Error!);
                }

                existingDates.Add((measuredAtUtc, type));
                generatedCount++;
            }

            return Result.Success(generatedCount);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Result.Failure<int>(ApplicationErrors.PersistenceUnavailable);
        }
    }

    private static decimal WeightVariation(int dayIndex)
        => ((dayIndex * 5) % 7) - 3;

    private static decimal SizeVariation(int dayIndex)
        => ((dayIndex * 7) % 21) - 10;

    private static decimal AbdomenVariation(int dayIndex)
        => ((dayIndex * 11 + 3) % 21) - 10;

    private static decimal HipVariation(int dayIndex)
        => ((dayIndex * 13 + 5) % 21) - 10;

}
