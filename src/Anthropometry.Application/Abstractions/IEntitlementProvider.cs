using Anthropometry.Application.Entitlements;

namespace Anthropometry.Application.Abstractions;

public interface IEntitlementProvider
{
    Task<EntitlementSnapshot> GetCurrentAsync(CancellationToken cancellationToken);
}
