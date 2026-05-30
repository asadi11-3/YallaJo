namespace Auth.Presentation.Endpoints.Session.Models;

/// <summary>
/// Phase 3A — admin-initiated password-reset request body.
/// <para>
/// The admin actor is resolved server-side from <c>ICurrentUser</c>, so
/// this DTO carries only the optional admin-facing reason string. The
/// target user id comes from the route template.
/// </para>
/// </summary>
public sealed record AdminResetPasswordRequest(string? Reason);
