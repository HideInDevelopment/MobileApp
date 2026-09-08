using Anthropometry.Application.Common;

namespace Anthropometry.App.Features.Measurements;

public interface IMeasurementNavigation
{
    Task ShowResultsAsync(MeasurementDto measurement);

    Task CancelAsync();
}
