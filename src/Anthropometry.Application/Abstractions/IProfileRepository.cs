using Anthropometry.Domain.Profiles;

namespace Anthropometry.Application.Abstractions;

public interface IProfileRepository
{
    Task<IReadOnlyList<Profile>> GetAllAsync(CancellationToken cancellationToken);

    Task<Profile?> GetByIdAsync(ProfileId id, CancellationToken cancellationToken);

    Task AddAsync(Profile profile, CancellationToken cancellationToken);

    Task UpdateAsync(Profile profile, CancellationToken cancellationToken);

    Task DeleteWithMeasurementsAsync(ProfileId id, CancellationToken cancellationToken);
}
