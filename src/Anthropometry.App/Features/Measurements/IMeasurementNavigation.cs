using Anthropometry.Application.Common;
using Anthropometry.App.Features.Help;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.App.Features.Measurements;

public interface IMeasurementNavigation
{
    Task ShowResultsAsync(MeasurementDto measurement);

    Task ShowResultsAsync(ProfileDto profile, MeasurementDto measurement);

    Task ShowHistoryAsync(ProfileDto profile);

    Task ShowChartOptionsAsync(ProfileId profileId);

    Task CloseMeasurementAsync();

    Task CancelAsync();

    Task ShowGuidanceAsync(GuidanceTopic topic);
}
