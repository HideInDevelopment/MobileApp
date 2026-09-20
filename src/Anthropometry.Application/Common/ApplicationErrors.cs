using Anthropometry.Domain.Common;

namespace Anthropometry.Application.Common;

internal static class ApplicationErrors
{
    public static DomainError ProfileNotFound => new("profile.notFound", "Errors.ProfileNotFound");

    public static DomainError ProfileLimitReached => new("profile.limit.reached", "Errors.ProfileLimitReached");

    public static DomainError PremiumFeatureRequired => new("premium.feature.required", "Errors.PremiumFeatureRequired");

    public static DomainError ProfileSettingsRequired => new("profile.settings.required", "Errors.ProfileSettingsRequired");

    public static DomainError CalculationUnavailableForMeasurementType => new("calculation.measurementType.unavailable", "Errors.CalculationUnavailableForMeasurementType");

    public static DomainError MeasurementNotFound => new("measurement.notFound", "Errors.MeasurementNotFound");

    public static DomainError MeasurementHistoryDateRangeInvalid => new("measurementHistory.dateRange.invalid", "Errors.MeasurementHistoryDateRangeInvalid");

    public static DomainError MeasurementPastDatePremiumRequired => new("measurement.pastDate.premiumRequired", "Errors.MeasurementPastDatePremiumRequired");

    public static DomainError MeasurementDateInvalid => new("measurement.date.invalid", "Errors.MeasurementDateInvalid");

    public static DomainError MetricHistoryDateRangeInvalid => new("metricHistory.dateRange.invalid", "Errors.MetricHistoryDateRangeInvalid");

    public static DomainError PersistenceUnavailable => new("persistence.unavailable", "Errors.PersistenceUnavailable");

    public static DomainError ProfileTransferFileInvalid => new("profile.transfer.file.invalid", "Errors.ProfileTransferFileInvalid");

    public static DomainError ProfileTransferFormatUnsupported => new("profile.transfer.format.unsupported", "Errors.ProfileTransferFormatUnsupported");

    public static DomainError ProfileTransferPasswordRequired => new("profile.transfer.password.required", "Errors.ProfileTransferPasswordRequired");

    public static DomainError ProfileTransferAuthenticationFailed => new("profile.transfer.authentication.failed", "Errors.ProfileTransferAuthenticationFailed");

    public static DomainError ProfileTransferPassphraseInvalid => new("profile.transfer.passphrase.invalid", "Errors.ProfileTransferPassphraseInvalid");
}
