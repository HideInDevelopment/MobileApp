using Anthropometry.Application.Abstractions;
using Anthropometry.Application.Entitlements;

namespace Anthropometry.Application.Tests.Entitlements;

public sealed class EntitlementServiceTests
{
    [Fact]
    public async Task Expired_cache_returns_free_access_without_deleting_local_data()
    {
        var cached = new EntitlementSnapshot(
            EntitlementTier.Premium,
            SubscriptionState.Expired,
            "anthropometry.premium.monthly",
            new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero));
        var cache = new FakeEntitlementCache(cached);
        var service = new EntitlementService(cache, new TestBillingGateway(throws: true), new TestClock());

        var snapshot = await service.GetCurrentAsync(CancellationToken.None);

        Assert.Equal(EntitlementTier.Free, snapshot.Tier);
        Assert.Same(cached, cache.Snapshot);
    }

    [Fact]
    public async Task Usable_cached_subscription_does_not_call_billing()
    {
        var cached = new EntitlementSnapshot(
            EntitlementTier.Premium,
            SubscriptionState.Active,
            "anthropometry.premium.monthly",
            new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 20, 0, 0, 0, TimeSpan.Zero));
        var billing = new TestBillingGateway(throws: false);
        var service = new EntitlementService(new FakeEntitlementCache(cached), billing, new TestClock());

        var snapshot = await service.GetCurrentAsync(CancellationToken.None);

        Assert.Same(cached, snapshot);
        Assert.Equal(0, billing.RestoreCalls);
    }

    private sealed class FakeEntitlementCache(EntitlementSnapshot? snapshot) : IEntitlementCache
    {
        public EntitlementSnapshot? Snapshot { get; private set; } = snapshot;

        public EntitlementSnapshot? Load() => Snapshot;

        public void Save(EntitlementSnapshot value) => Snapshot = value;
    }

    private sealed class TestBillingGateway(bool throws) : IBillingGateway
    {
        public int RestoreCalls { get; private set; }

        public Task<BillingCatalog> GetCatalogAsync(CancellationToken cancellationToken)
            => throws
                ? throw new InvalidOperationException("Billing unavailable")
                : Task.FromResult(new BillingCatalog([]));

        public Task<BillingPurchaseResult> PurchaseAsync(string productId, string basePlanId, CancellationToken cancellationToken)
            => throws
                ? throw new InvalidOperationException("Billing unavailable")
                : Task.FromResult(new BillingPurchaseResult(BillingPurchaseState.Canceled, null));

        public Task<EntitlementSnapshot?> RestoreAsync(CancellationToken cancellationToken)
        {
            RestoreCalls++;
            return throws
                ? throw new InvalidOperationException("Billing unavailable")
                : Task.FromResult<EntitlementSnapshot?>(null);
        }

        public void OpenManageSubscription()
        {
        }
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 9, 20, 12, 0, 0, TimeSpan.Zero);
    }
}
