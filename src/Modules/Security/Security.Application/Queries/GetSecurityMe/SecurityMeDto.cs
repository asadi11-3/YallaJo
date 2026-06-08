namespace Security.Application.Queries.GetSecurityMe;

/// <summary>
/// DB-backed snapshot of the current user's identity, roles and effective
/// permissions. Drives the admin shell navigation (plan §9 line 13: nav driver
/// = GET /security/me, DB-backed — NOT the raw JWT claims).
/// </summary>
public sealed record SecurityMeDto(
    Guid UserId,
    string Email,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);
