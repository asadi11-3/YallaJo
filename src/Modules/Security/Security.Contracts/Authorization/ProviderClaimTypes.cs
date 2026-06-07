namespace Security.Contracts.Authorization;

/// <summary>
/// Server-generated identity claim types that are embedded into the access-token JWT
/// (via the user's persisted <c>UserClaim</c> rows) and consumed by other modules.
/// </summary>
/// <remarks>
/// These values must NEVER be trusted from a client request. They are resolved
/// server-side from the approved provider record and persisted as <c>UserClaim</c>s,
/// then surfaced on the JWT by <c>SecurityService</c> when the user authenticates.
/// </remarks>
public static class ProviderClaimTypes
{
    /// <summary>
    /// Claim type carrying the approved provider's identifier
    /// (the <c>ProviderApplication.Id</c>, which is the canonical ProviderId used
    /// across Booking, Finance and ContentTours). Issued when a provider application
    /// is approved; consumed by provider-scoped Finance endpoints
    /// (e.g. <c>/api/v1/invoices/provider/my-invoices</c>, <c>/api/v1/payouts/provider</c>).
    /// </summary>
    public const string ProviderId = "provider_id";
}
