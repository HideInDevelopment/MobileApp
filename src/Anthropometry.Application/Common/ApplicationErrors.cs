using Anthropometry.Domain.Common;

namespace Anthropometry.Application.Common;

internal static class ApplicationErrors
{
    public static DomainError ProfileNotFound => new("profile.notFound", "Errors.ProfileNotFound");

    public static DomainError MeasurementNotFound => new("measurement.notFound", "Errors.MeasurementNotFound");

    public static DomainError PersistenceUnavailable => new("persistence.unavailable", "Errors.PersistenceUnavailable");
}
