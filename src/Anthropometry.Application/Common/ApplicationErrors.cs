using Anthropometry.Domain.Common;

namespace Anthropometry.Application.Common;

internal static class ApplicationErrors
{
    public static DomainError ProfileNotFound => new("profile.notFound", "Errors.ProfileNotFound");

    public static DomainError ProfileLimitReached => new("profile.limit.reached", "Errors.ProfileLimitReached");

    public static DomainError ProfileSettingsRequired => new("profile.settings.required", "Errors.ProfileSettingsRequired");

    public static DomainError CalculationUnavailableForMeasurementType => new("calculation.measurementType.unavailable", "Errors.CalculationUnavailableForMeasurementType");

    public static DomainError SampleDataRequiresSizeMeasurement => new("sampleData.sizeMeasurement.required", "Errors.SampleDataRequiresSizeMeasurement");

    public static DomainError MeasurementNotFound => new("measurement.notFound", "Errors.MeasurementNotFound");

    public static DomainError PersistenceUnavailable => new("persistence.unavailable", "Errors.PersistenceUnavailable");
}
