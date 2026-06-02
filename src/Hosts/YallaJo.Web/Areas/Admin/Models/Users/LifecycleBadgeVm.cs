namespace YallaJo.Web.Areas.Admin.Models.Users;

/// <summary>
/// Phase 5D — input record for the <c>_LifecycleBadge</c> partial.
/// <para>
/// Today the partial only consumes <see cref="IsActive"/> (binary
/// Active/Inactive badge) because the backend <c>UserDto</c> does not
/// yet expose a granular <c>LifecycleState</c>.
/// </para>
/// <para>
/// <see cref="LifecycleState"/> is accepted as an optional parameter
/// for forward-compatibility: when the read API is later extended
/// (e.g. with <c>"Suspended"</c>, <c>"PendingActivation"</c>,
/// <c>"PendingPasswordReset"</c>, <c>"Provisioned"</c>,
/// <c>"Archived"</c>), only the <see cref="Helpers.LifecycleBadge"/>
/// helper changes — every call site is already passing the field.
/// </para>
/// </summary>
public sealed record LifecycleBadgeVm(bool IsActive, string? LifecycleState = null);
