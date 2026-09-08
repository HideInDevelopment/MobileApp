using Anthropometry.Application.Abstractions;
using Anthropometry.Application.Common;
using Anthropometry.Domain.Common;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.Application.Profiles;

public sealed class RenameProfile
{
    private readonly IProfileRepository _repository;
    private readonly IClock _clock;

    public RenameProfile(IProfileRepository repository, IClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public async Task<Result<ProfileDto>> ExecuteAsync(ProfileId id, string name, CancellationToken cancellationToken)
    {
        try
        {
            var profile = await _repository.GetByIdAsync(id, cancellationToken);
            if (profile is null)
            {
                return Result.Failure<ProfileDto>(ApplicationErrors.ProfileNotFound);
            }

            var renamed = profile.Rename(name, _clock.UtcNow);
            if (!renamed.IsSuccess)
            {
                return Result.Failure<ProfileDto>(renamed.Error!);
            }

            await _repository.UpdateAsync(profile, cancellationToken);
            return Result.Success(ApplicationModels.ToDto(profile));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Result.Failure<ProfileDto>(ApplicationErrors.PersistenceUnavailable);
        }
    }
}
