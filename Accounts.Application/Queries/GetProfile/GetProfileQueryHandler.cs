using Accounts.Domain.Errors;
using Accounts.Domain.Repositories;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Queries.GetProfile;

public sealed class GetProfileQueryHandler(
    IProfileRepository profileRepository,
    ISecurityService securityService,
    ICurrentUser currentUser)
    : IQueryHandler<GetProfileQuery, GetProfileResult>
{
    public async Task<Result<GetProfileResult>> Handle(
        GetProfileQuery request,
        CancellationToken cancellationToken)
    {.
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Result<GetProfileResult>.Failure(
                Error.Unauthorized("Authentication is required."),
                Outcome.Unauthorized);
        }

        if (currentUser.UserId.Value != request.UserId)
        {
            return Result<GetProfileResult>.Failure(
                Error.Forbidden("You may only access your own profile."),
                Outcome.Forbidden);
        }

        var userId = request.UserId;

        var profile = await profileRepository.FirstOrDefaultAsync(
            filter: p => p.UserId == userId,
            ct: cancellationToken);

        if (profile is null)
        {
            return Result<GetProfileResult>.Failure(
                ProfileErrors.NotFound,
                Outcome.NotFound);
        }

        var contact = await securityService.GetPrimaryContactDataAsync(userId, cancellationToken);
        if (contact is null)
        {
            return Result<GetProfileResult>.Failure(
                Error.NotFound("User", "User not found."),
                Outcome.NotFound);
        }

        var result = new GetProfileResult(
            UserId: profile.UserId,
            FirstName: profile.FirstName,
            LastName: profile.LastName,
            DisplayName: profile.DisplayName,
            AvatarUrl: profile.AvatarUrl,
            PhoneNumber: contact.PhoneNumber,
            DateOfBirth: profile.DateOfBirth,
            Gender: profile.Gender?.ToString(),
            Country: profile.Country,
            City: profile.City,
            AddressLine: profile.AddressLine,
            Email: contact.Email);

        return Result<GetProfileResult>.Success(result);
    }
}
