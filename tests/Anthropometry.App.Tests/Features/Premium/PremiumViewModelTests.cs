using Anthropometry.Application.Abstractions;
using Anthropometry.Application.Entitlements;
using Anthropometry.App.Features.Premium;
using Anthropometry.App.Tests.Support;

namespace Anthropometry.App.Tests.Features.Premium;

public sealed class PremiumViewModelTests
{
    [Fact]
    public async Task Billing_error_keeps_free_access_and_exposes_recoverable_message()
    {
        var billing = new TestBillingGateway(throws: true);
        var viewModel = CreateViewModel(billing);

        await viewModel.SubscribeCommand.ExecuteAsync(null);

        Assert.False(viewModel.IsPremium);
        Assert.False(string.IsNullOrWhiteSpace(viewModel.ErrorMessage));
    }

    [Fact]
    public async Task Successful_purchase_updates_the_local_entitlement()
    {
        var entitlement = new EntitlementSnapshot(
            EntitlementTier.Premium,
            SubscriptionState.Active,
            "anthropometry.premium.monthly",
            null,
            null);
        var viewModel = CreateViewModel(new TestBillingGateway(entitlement));

        await viewModel.SubscribeCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsPremium);
        Assert.Equal(EntitlementTier.Premium, viewModel.Tier);
    }

    private static PremiumViewModel CreateViewModel(TestBillingGateway billing)
    {
        var cache = new TestEntitlementCache(new EntitlementSnapshot(
            EntitlementTier.Free,
            SubscriptionState.Active,
            null,
            null,
            null));
        var service = new EntitlementService(cache, billing, new FakeClock());
        return new PremiumViewModel(service, billing, TestData.LanguageService());
    }

    private sealed class TestEntitlementCache(EntitlementSnapshot snapshot) : IEntitlementCache
    {
        public EntitlementSnapshot? Snapshot { get; private set; } = snapshot;

        public EntitlementSnapshot? Load() => Snapshot;

        public void Save(EntitlementSnapshot value) => Snapshot = value;
    }

    private sealed class TestBillingGateway : IBillingGateway
    {
        private readonly EntitlementSnapshot? _purchaseEntitlement;
        private readonly bool _throws;

        public TestBillingGateway(EntitlementSnapshot? purchaseEntitlement = null, bool throws = false)
        {
            _purchaseEntitlement = purchaseEntitlement;
            _throws = throws;
        }

        public Task<BillingCatalog> GetCatalogAsync(CancellationToken cancellationToken)
            => _throws
                ? throw new InvalidOperationException("Billing unavailable")
                : Task.FromResult(new BillingCatalog([
                    new BillingOffer("anthropometry.premium.monthly", "monthly", "Monthly", "€1.99")
                ]));

        public Task<BillingPurchaseResult> PurchaseAsync(string productId, string basePlanId, CancellationToken cancellationToken)
            => _throws
                ? throw new InvalidOperationException("Billing unavailable")
                : Task.FromResult(new BillingPurchaseResult(
                    _purchaseEntitlement is null ? BillingPurchaseState.Canceled : BillingPurchaseState.Purchased,
                    _purchaseEntitlement));

        public Task<EntitlementSnapshot?> RestoreAsync(CancellationToken cancellationToken)
            => Task.FromResult<EntitlementSnapshot?>(null);

        public void OpenManageSubscription()
        {
        }
    }
}
