using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Application.Queries.GetSecurityMe;

/// <summary>
/// Returns the current user's DB-backed roles &amp; permissions snapshot
/// (the admin-shell navigation driver, plan §9 line 13).
/// </summary>
public sealed record GetSecurityMeQuery(Guid UserId) : IQuery<SecurityMeDto>;
