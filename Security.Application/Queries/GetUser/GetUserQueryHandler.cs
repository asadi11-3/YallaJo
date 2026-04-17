using Security.Application.Queries.Dtos;
using Security.Domain.Errors;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Queries.GetUser;

public sealed class GetUserQueryHandler(IUserRepository userRepository)
    : IQueryHandler<GetUserQuery, UserDto>
{
    public async Task<Result<UserDto>> Handle(GetUserQuery request, CancellationToken ct)
    {
        var user = await userRepository.GetByIdWithDetailsAsync(request.UserId, ct);
        if (user is null)
            return Result<UserDto>.Failure(UserErrors.NotFound, Outcome.NotFound);

        var primaryEmail = user.GetPrimaryEmail();

        var roles = user.UserRoles
            .Where(ur => ur.Role.IsActive)
            .Select(ur => ur.Role.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var claims = user.UserClaims
            .Select(uc => new UserClaimDto(uc.Id, uc.ClaimType, uc.ClaimValue))
            .ToList();

        return Result<UserDto>.Success(new UserDto(
            user.Id,
            primaryEmail?.Address ?? string.Empty,
            user.IsActive,
            roles,
            claims));
    }
}
