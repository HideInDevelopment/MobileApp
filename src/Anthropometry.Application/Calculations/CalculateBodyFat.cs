using Anthropometry.Application.Abstractions;
using Anthropometry.Application.Common;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Calculations.BodyFat;
using Anthropometry.Domain.Common;
using Anthropometry.Domain.Measurements;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.Application.Calculations;

public sealed record CalculateBodyFatCommand(ProfileId ProfileId, MeasurementId MeasurementId);

public sealed class CalculateBodyFat
{
    private readonly IMeasurementRepository _measurements;
    private readonly ICalculationResultRepository _results;
    private readonly IFormulaCatalog _catalog;
    private readonly IClock _clock;

    public CalculateBodyFat(IMeasurementRepository measurements, ICalculationResultRepository results, IFormulaCatalog catalog, IClock clock)
    {
        _measurements = measurements;
        _results = results;
        _catalog = catalog;
        _clock = clock;
    }

    public async Task<Result<CalculationResultDto>> ExecuteAsync(CalculateBodyFatCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var measurement = await _measurements.GetByIdAsync(command.MeasurementId, cancellationToken);
            if (measurement is null || measurement.ProfileId != command.ProfileId)
            {
                return Result.Failure<CalculationResultDto>(ApplicationErrors.MeasurementNotFound);
            }

            var calculated = _catalog.BodyFat.Calculate(new BodyFatInput(measurement.AbdomenCm, measurement.NeckCm, measurement.HeightCm));
            if (!calculated.IsSuccess)
            {
                return Result.Failure<CalculationResultDto>(calculated.Error!);
            }

            var result = CalculationResult.Create(measurement.Id, CalculationType.BodyFatPercentage, calculated.Value, _clock.UtcNow);
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
