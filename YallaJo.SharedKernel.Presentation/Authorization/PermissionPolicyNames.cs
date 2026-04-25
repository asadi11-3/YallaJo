namespace YallaJo.SharedKernel.Presentation.Authorization;

/// <summary>
/// Single source of truth for the permission policy name format.
/// Attribute + runtime handler both use this — decoupled from any assembly move.
/// </summary>
public static class PermissionPolicyNames
{
    public const string Prefix = "Permission.";

    public static string Build(string feature, string action)
        => $"{Prefix}{feature}.{action}";

    public static bool IsPermissionPolicy(string policyName)
        => policyName is not null
           && policyName.StartsWith(Prefix, StringComparison.Ordinal);
}
