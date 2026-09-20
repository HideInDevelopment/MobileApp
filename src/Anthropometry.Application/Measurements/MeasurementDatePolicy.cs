using Anthropometry.Application.Entitlements;
using Anthropometry.Application.Common;
using Anthropometry.Domain.Common;

namespace Anthropometry.Application.Measurements;

public static class MeasurementDatePolicy
{
    public static Result Validate(
        DateTimeOffset measuredAtUtc,
        EntitlementSnapshot entitlement,
        DateTimeOffset nowUtc)
    {
        var measuredDate = measuredAtUtc.ToLocalTime().Date;
        var currentDate = nowUtc.ToLocalTime().Date;
        if (measuredDate > currentDate)
        {
            return Result.Failure(ApplicationErrors.MeasurementDateInvalid);
        }

        if (measuredDate < currentDate
            && !FeatureAccessPolicy.CanUse(entitlement, PremiumFeature.PastMeasurements))
        {
            return Result.Failure(ApplicationErrors.MeasurementPastDatePremiumRequired);
        }

        return Result.Success();
    }
}
