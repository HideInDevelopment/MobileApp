using Anthropometry.Application.Abstractions;
using Anthropometry.Application.Common;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Calculations.Bmr;
using Anthropometry.Domain.Calculations.Tdee;
using Anthropometry.Domain.Common;
using Anthropometry.Domain.Measurements;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.Application.Calculations;

public sealed record CalculateTdeeCommand(ProfileId ProfileId, MeasurementId MeasurementId);

public sealed class CalculateTotalDailyEnergyExpenditure
{
    private readonly IMeasurementRepository _measurements;
    private readonly ICalculationResultRepository _results;
    private readonly IFormulaCatalog _catalog;
    private readonly IClock _clock;

    public CalculateTotalDailyEnergyExpenditure(IMeasurementRepository measurements, ICalculationResultRepository results, IFormulaCatalog catalog, IClock clock)
    {
        _measurements = measurements;
        _results = results;
        _catalog = catalog;
        _clock = clock;
    }

    public async Task<Result<CalculationResultDto>> ExecuteAsync(CalculateTdeeCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var measurement = await _measurements.GetByIdAsync(command.MeasurementId, cancellationToken);
            if (measurement is null || measurement.ProfileId != command.ProfileId)
            {
                return Result.Failure<CalculationResultDto>(ApplicationErrors.MeasurementNotFound);
            }

            var sizeSource = await MeasurementCalculationContext.GetSizeSourceAsync(
                _measurements,
                command.ProfileId,
                measurement,
                cancellationToken);
            if (sizeSource is null)
            {
                return Result.Failure<CalculationResultDto>(ApplicationErrors.CalculationUnavailableForMeasurementType);
            }

            var bmrFormula = measurement.Gender == ProfileGender.Female
                ? _catalog.FemaleBmr
                : _catalog.MaleBmr;
            var bmr = bmrFormula.Calculate(new BmrInput(measurement.WeightKg, measurement.HeightCm, measurement.AgeYears));
            if (!bmr.IsSuccess)
            {
                return Result.Failure<CalculationResultDto>(bmr.Error!);
            }

            var calculated = _catalog.Tdee.Calculate(new TdeeInput(bmr.Value.Value, measurement.ActivityLevel));
            if (!calculated.IsSuccess)
            {
                return Result.Failure<CalculationResultDto>(calculated.Error!);
            }

            var result = CalculationResult.Create(measurement.Id, CalculationType.TotalDailyEnergyExpenditure, calculated.Value, _clock.UtcNow);
            if (!result.IsSuccess)
            {
                return Result.Failure<CalculationResultDto>(result.Error!);
            }

            await _results.AddAsync(result.Value, cancellationToken);
            return Result.Success(ApplicationModels.ToDto(result.Value));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Result.Failure<CalculationResultDto>(ApplicationErrors.PersistenceUnavailable);
        }
    }
}
