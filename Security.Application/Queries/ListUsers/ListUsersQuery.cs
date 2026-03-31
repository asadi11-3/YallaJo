using Security.Application.Queries.Dtos;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;

namespace Security.Application.Queries.ListUsers;

public sealed record ListUsersQuery(
    int Page = 1,
    int PageSize = 20) : IQuery<PaginatedResult<UserDto>>;
