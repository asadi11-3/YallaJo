using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.InviteUser;

public sealed record InviteUserResult(Guid UserId, Guid ProfileId);

public sealed record InviteUserCommand(
    string Email,
    string FirstName,
    string LastName,
    string? DisplayName,
    string? AvatarUrl,
    IReadOnlyList<Guid> InitialRoleIds) : ICommand<InviteUserResult>;
