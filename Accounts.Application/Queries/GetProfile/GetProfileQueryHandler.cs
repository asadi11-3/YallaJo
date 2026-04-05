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
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Result<GetProfileResult>.Failure(
                Error.Unauthorized("Authentication is required."),
                Outcome.Unauthorized);
        }

        // استخدام الـ UserId الممرر من الـ Query (والذي يجب أن يتطابق مع المستخدم الحالي)
        var userId = request.UserId;
        var email = currentUser.Email ?? string.Empty;

        var profile = await profileRepository.FirstOrDefaultAsync(
            filter: p => p.UserId == userId,
            ct: cancellationToken);

        if (profile is null)
        {
            return Result<GetProfileResult>.Failure(
                Error.NotFound("Profile", "Profile not found."),
                Outcome.NotFound);
        }

        var phoneNumber = await securityService.GetPrimaryPhoneNumberAsync(userId, cancellationToken);

        var result = new GetProfileResult(
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
            Email: email);

        return Result<GetProfileResult>.Success(result);
    }
}
