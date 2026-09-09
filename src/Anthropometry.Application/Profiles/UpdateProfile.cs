using Anthropometry.Application.Abstractions;
using Anthropometry.Application.Common;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Common;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.Application.Profiles;

public sealed class UpdateProfile
{
    private readonly IProfileRepository _repository;
    private readonly IClock _clock;

    public UpdateProfile(IProfileRepository repository, IClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public async Task<Result<ProfileDto>> ExecuteAsync(
        ProfileId id,
        string name,
        ProfileSettingsInput settingsInput,
        CancellationToken cancellationToken)
    {
        try
        {
            var profile = await _repository.GetByIdAsync(id, cancellationToken);
            if (profile is null)
            {
                return Result.Failure<ProfileDto>(ApplicationErrors.ProfileNotFound);
            }

            var settings = ProfileSettings.Create(settingsInput.HeightCm, settingsInput.AgeYears, settingsInput.ActivityLevel);
            if (!settings.IsSuccess)
            {
                return Result.Failure<ProfileDto>(settings.Error!);
            }

            var updated = profile.Update(name, settings.Value, _clock.UtcNow);
            if (!updated.IsSuccess)
            {
                return Result.Failure<ProfileDto>(updated.Error!);
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
