using Security.Application.Queries.Dtos;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Application.Queries.GetUser;
public sealed record GetUserQuery(Guid UserId) : IQuery<UserDto>;
