using Anthropometry.Application.Entitlements;

namespace Anthropometry.Application.Abstractions;

public interface IBillingGateway
{
    Task<BillingCatalog> GetCatalogAsync(CancellationToken cancellationToken);

    Task<BillingPurchaseResult> PurchaseAsync(
        string productId,
        string basePlanId,
        CancellationToken cancellationToken);

    Task<EntitlementSnapshot?> RestoreAsync(CancellationToken cancellationToken);

    void OpenManageSubscription();
}
