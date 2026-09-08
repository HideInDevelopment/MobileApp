using Anthropometry.Application.Common;

namespace Anthropometry.App.Features.Profiles;

public interface IProfileNavigation
{
    Task CreateProfileAsync();

    Task RenameProfileAsync(ProfileDto profile);

    Task SelectProfileAsync(ProfileDto profile);

    Task<bool> ConfirmDeleteAsync(ProfileDto profile);

    Task CloseEditorAsync(ProfileDto profile);

    Task CreateMeasurementAsync(ProfileDto profile);

    Task CancelAsync();

    Task ShowHistoryAsync(ProfileDto profile);
}
