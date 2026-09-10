using Anthropometry.Application.Common;
using Anthropometry.Domain.Measurements;

namespace Anthropometry.App.Features.Profiles;

public interface IProfileNavigation
{
    Task CreateProfileAsync();

    Task RenameProfileAsync(ProfileDto profile);

    Task SelectProfileAsync(ProfileDto profile);

    Task<bool> ConfirmDeleteAsync(ProfileDto profile);

    Task CloseEditorAsync(ProfileDto profile);

    Task CreateMeasurementAsync(ProfileDto profile, MeasurementType type);

    Task CancelAsync();

    Task ShowHistoryAsync(ProfileDto profile);

    Task ShowSettingsAsync();
}
