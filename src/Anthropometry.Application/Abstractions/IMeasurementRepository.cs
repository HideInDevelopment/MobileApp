using Anthropometry.Domain.Measurements;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.Application.Abstractions;

public interface IMeasurementRepository
{
    Task AddAsync(Measurement measurement, CancellationToken cancellationToken);

    Task<Measurement?> GetByIdAsync(MeasurementId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Measurement>> GetByProfileAsync(ProfileId profileId, CancellationToken cancellationToken);
}
