using Anthropometry.Application.Entitlements;
using Anthropometry.Application.Measurements;

namespace Anthropometry.Application.Tests.Measurements;

public sealed class MeasurementDatePolicyTests
{
    [Fact]
    public void Free_rejects_a_past_local_date()
    {
        var now = new DateTimeOffset(2026, 9, 20, 12, 0, 0, TimeSpan.Zero);

        var result = MeasurementDatePolicy.Validate(
            now.AddDays(-1),
            new EntitlementSnapshot(EntitlementTier.Free, SubscriptionState.Active, null, null, null),
            now);

        Assert.False(result.IsSuccess);
        Assert.Equal("measurement.pastDate.premiumRequired", result.Error!.Code);
    }

    [Fact]
    public void Free_accepts_the_current_local_date()
    {
        var now = new DateTimeOffset(2026, 9, 20, 12, 0, 0, TimeSpan.Zero);

        var result = MeasurementDatePolicy.Validate(
            now.AddHours(-6),
            new EntitlementSnapshot(EntitlementTier.Free, SubscriptionState.Active, null, null, null),
            now);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Premium_accepts_a_past_date_but_rejects_a_future_date()
    {
        var now = new DateTimeOffset(2026, 9, 20, 12, 0, 0, TimeSpan.Zero);
        var entitlement = new EntitlementSnapshot(
            EntitlementTier.Premium,
            SubscriptionState.Active,
            "anthropometry.premium.monthly",
            null,
            null);
        Assert.True(MeasurementDatePolicy.Validate(now.AddDays(-1), entitlement, now).IsSuccess);
        Assert.False(MeasurementDatePolicy.Validate(now.AddDays(1), entitlement, now).IsSuccess);
        Assert.Equal("measurement.date.invalid", MeasurementDatePolicy.Validate(now.AddDays(1), entitlement, now).Error!.Code);
    }
}
