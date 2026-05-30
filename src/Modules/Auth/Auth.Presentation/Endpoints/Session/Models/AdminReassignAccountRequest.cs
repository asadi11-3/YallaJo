namespace Auth.Presentation.Endpoints.Session.Models;

/// <summary>
/// Phase 3C — admin-initiated account-reassignment request body.
/// <para>
/// The target user id comes from the route template; the admin actor
/// is resolved server-side from <c>ICurrentUser</c>. This DTO carries
/// the new primary email and an optional admin-facing reason string.
/// </para>
/// </summary>
public sealed record AdminReassignAccountRequest(string NewEmail, string? Reason);
