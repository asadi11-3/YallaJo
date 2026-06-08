using Security.Contracts.Abstractions;
using Security.Domain.Errors;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Queries.GetSecurityMe;

public sealed class GetSecurityMeQueryHandler(ISecurityService securityService)
    : IQueryHandler<GetSecurityMeQuery, SecurityMeDto>
{
    private const string PermissionClaimType = "Permission";

    public async Task<Result<SecurityMeDto>> Handle(
        GetSecurityMeQuery request, CancellationToken cancellationToken)
    {
        var data = await securityService.GetUserDataByIdAsync(request.UserId, cancellationToken);
        if (data is null)
            return Result<SecurityMeDto>.Failure(UserErrors.NotFound, Outcome.NotFound);

        var permissions = data.Claims
            .Where(c => string.Equals(c.Type, PermissionClaimType, StringComparison.Ordinal))
            .Select(c => c.Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return Result<SecurityMeDto>.Success(
            new SecurityMeDto(data.UserId, data.Email, data.Roles, permissions));
    }
}
