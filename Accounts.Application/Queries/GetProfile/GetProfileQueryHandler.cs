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
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null) {
            return Result<GetProfileResult>.Failure(
                   Error.Unauthorized("Authentication is required."),
                   Outcome.Unauthorized);
        }

        var profile = await profileRepository.GetByUserIdAsync(currentUser.UserId.Value, cancellationToken);
        if (profile is null) {
            return Result<GetProfileResult>.Failure(
                   Error.NotFound("Profile", "Profile not found."),
                   Outcome.NotFound);
        }

        var phoneNumber = await securityService.GetPrimaryPhoneNumberAsync(currentUser.UserId.Value, cancellationToken);

        return Result<GetProfileResult>.Success(new GetProfileResult(
            UserId: profile.UserId,
            FirstName: profile.FirstName,
            LastName: profile.LastName,
            DisplayName: profile.DisplayName,
            AvatarUrl: profile.AvatarUrl,
            PhoneNumber: phoneNumber,
            DateOfBirth: profile.DateOfBirth,
            Gender: profile.Gender?.ToString(),
            Country: profile.Country,
            City: profile.City,
            AddressLine: profile.AddressLine,
            Email: currentUser.Email ?? string.Empty));
    }
}
