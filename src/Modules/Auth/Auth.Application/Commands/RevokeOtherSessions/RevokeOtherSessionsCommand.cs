using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.RevokeOtherSessions;

/// <summary>
/// Revokes all of the caller's active sessions except the current one
/// (identified by the JWT "sid" claim, passed in by the endpoint).
/// Additive companion to <see cref="LogoutAll.LogoutAllCommand"/>, which
/// revokes everything including the current session.
/// </summary>
public sealed record RevokeOtherSessionsCommand(Guid CurrentSessionId)
    : ICommand<RevokeOtherSessionsResult>;

public sealed record RevokeOtherSessionsResult(int RevokedCount);
