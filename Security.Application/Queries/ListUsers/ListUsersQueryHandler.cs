using Security.Application.Queries.GetUser;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Queries.ListUsers;

public sealed class ListUsersQueryHandler(IUserRepository userRepository)
    : IQueryHandler<ListUsersQuery, PaginatedResult<UserDto>>
{
    public async Task<Result<PaginatedResult<UserDto>>> Handle(
        ListUsersQuery request, CancellationToken ct)
    {
        var (items, total) = await userRepository.GetPagedWithDetailsAsync(
            request.Page, request.PageSize, ct);

        var dtos = items.Select(user =>
        {
            var primaryEmail = user.GetPrimaryEmail();
            var roles = user.UserRoles
                .Where(ur => ur.Role.IsActive)
                .Select(ur => ur.Role.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new UserDto(
                user.Id,
                primaryEmail?.Address ?? string.Empty,
                user.IsActive,
                roles);
        }).ToList();

        return Result<PaginatedResult<UserDto>>.Success(
            new PaginatedResult<UserDto>(dtos, total, request.Page, request.PageSize));
    }
}
