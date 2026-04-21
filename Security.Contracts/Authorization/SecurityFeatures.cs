namespace Security.Contracts.Authorization;

/// <summary>
/// Feature string constants owned by the Security bounded context.
/// Other modules MUST NOT add constants here — each module owns its own Features file.
/// </summary>
public static class SecurityFeatures
{
    public const string Role      = nameof(Role);
    public const string UserRole  = nameof(UserRole);
    public const string RoleClaim = nameof(RoleClaim);
    public const string User      = nameof(User);
    public const string System    = nameof(System);
}
