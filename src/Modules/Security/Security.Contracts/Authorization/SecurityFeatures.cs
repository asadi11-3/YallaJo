namespace Security.Contracts.Authorization;

public static class SecurityFeatures
{
    public const string Role      = nameof(Role);
    public const string UserRole  = nameof(UserRole);
    public const string RoleClaim = nameof(RoleClaim);
    public const string User      = nameof(User);
    public const string System    = nameof(System);
    public const string AuditLog  = nameof(AuditLog);

    /// <summary>
    /// Operational outbox dead-letter management. Mirrors
    /// <c>YallaJo.SharedKernel.Application.Authorization.OpsFeatures.Outbox</c>
    /// (both resolve to the literal "Outbox") so the catalog seeds the exact
    /// permission strings the /ops/outbox endpoints enforce.
    /// </summary>
    public const string Outbox    = nameof(Outbox);
}
