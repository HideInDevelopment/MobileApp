using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Measurements;

namespace Anthropometry.Application.Abstractions;

public interface ICalculationResultRepository
{
    Task AddAsync(CalculationResult result, CancellationToken cancellationToken);

    Task DeleteByMeasurementAsync(MeasurementId measurementId, CancellationToken cancellationToken);

    Task<IReadOnlyList<CalculationResult>> GetByMeasurementAsync(MeasurementId measurementId, CancellationToken cancellationToken);
}
