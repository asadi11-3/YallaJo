using Security.Application.Queries.Dtos;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Application.Queries.GetRole;

public sealed record GetRoleQuery(Guid RoleId) : IQuery<RoleDetailsDto>;
