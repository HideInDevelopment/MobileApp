using Anthropometry.Application.Abstractions;
using Anthropometry.Application.Common;
using Anthropometry.Domain.Common;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.Application.Profiles;

public sealed class CreateProfile
{
    private readonly IProfileRepository _repository;
    private readonly IClock _clock;

    public CreateProfile(IProfileRepository repository, IClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public async Task<Result<ProfileDto>> ExecuteAsync(string name, CancellationToken cancellationToken)
    {
        var created = Profile.Create(name, _clock.UtcNow);
        if (!created.IsSuccess)
        {
            return Result.Failure<ProfileDto>(created.Error!);
        }

        try
        {
            await _repository.AddAsync(created.Value, cancellationToken);
            return Result.Success(ApplicationModels.ToDto(created.Value));
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
