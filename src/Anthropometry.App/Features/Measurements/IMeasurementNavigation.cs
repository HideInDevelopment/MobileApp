using Anthropometry.Application.Common;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.App.Features.Measurements;

public interface IMeasurementNavigation
{
    Task ShowResultsAsync(MeasurementDto measurement);

    Task ShowHistoryAsync(ProfileDto profile);

    Task ShowChartOptionsAsync(ProfileId profileId);

    Task CloseMeasurementAsync();

    Task CancelAsync();
}
