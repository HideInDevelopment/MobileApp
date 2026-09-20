using Anthropometry.Application.Abstractions;
using Anthropometry.Application.Entitlements;

namespace Anthropometry.App.Features.Premium;

public sealed class UnavailableBillingGateway : IBillingGateway
{
    public Task<BillingCatalog> GetCatalogAsync(CancellationToken cancellationToken)
        => throw new InvalidOperationException("Google Play Billing is unavailable.");

    public Task<BillingPurchaseResult> PurchaseAsync(
        string productId,
        string basePlanId,
        CancellationToken cancellationToken)
        => throw new InvalidOperationException("Google Play Billing is unavailable.");

    public Task<EntitlementSnapshot?> RestoreAsync(CancellationToken cancellationToken)
        => throw new InvalidOperationException("Google Play Billing is unavailable.");

    public void OpenManageSubscription()
    {
    }
}
