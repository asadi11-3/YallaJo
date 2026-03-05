using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Application.Queries.GetUser;

public sealed record UserDto(
    Guid Id,
    string Email,
    bool IsActive,
    IReadOnlyList<string> Roles);

public sealed record GetUserQuery(Guid UserId) : IQuery<UserDto>;
