namespace YallaJo.Web.Areas.Admin.Models.Lifecycle;

/// <summary>
/// Phase 5B — wire DTO posted by the Web admin UI to the Phase 3A
/// admin-initiated password-reset endpoint
/// (<c>POST /api/v1/auth/admin/users/{id}/reset-password</c>).
/// <para>
/// The admin actor is resolved server-side from the bearer token, so
/// only the optional reason is carried over the wire.
/// </para>
/// </summary>
public sealed class AdminResetPasswordRequest
{
    public string? Reason { get; init; }
}
