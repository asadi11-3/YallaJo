namespace YallaJo.SharedKernel.Application.Authorization;

/// <summary>
/// Feature names for operational (system-admin) permissions.
/// These are cross-cutting concerns that live in SharedKernel.
/// </summary>
public static class OpsFeatures
{
    /// <summary>Access to outbox dead-letter management (view + replay).</summary>
    public const string Outbox = nameof(Outbox);
}
