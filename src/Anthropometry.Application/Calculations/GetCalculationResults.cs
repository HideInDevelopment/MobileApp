using Anthropometry.Application.Abstractions;
using Anthropometry.Application.Common;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Common;
using Anthropometry.Domain.Measurements;

namespace Anthropometry.Application.Calculations;

public sealed class GetCalculationResults
{
    private readonly ICalculationResultRepository _repository;

    public GetCalculationResults(ICalculationResultRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<CalculationResultDto>>> ExecuteAsync(MeasurementId measurementId, CancellationToken cancellationToken)
    {
        try
        {
            var results = await _repository.GetByMeasurementAsync(measurementId, cancellationToken);
            return Result.Success<IReadOnlyList<CalculationResultDto>>(results.Select(ApplicationModels.ToDto).ToArray());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Result.Failure<IReadOnlyList<CalculationResultDto>>(ApplicationErrors.PersistenceUnavailable);
        }
    }
}
