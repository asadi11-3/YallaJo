using Accounts.Application.Caching;
using Accounts.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Queries.GetProfile;

public sealed class GetProfileQueryHandler(
    IProfileRepository profileRepository,
    ISecurityService securityService,
    ICurrentUser currentUser,
    HybridCache cache)
    : IQueryHandler<GetProfileQuery, GetProfileResult>
{
    private static readonly HybridCacheEntryOptions _cacheOptions = new()
    {
        Expiration           = TimeSpan.FromMinutes(10),
        LocalCacheExpiration = TimeSpan.FromMinutes(2)
    };

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

        var userId = currentUser.UserId.Value;
        var email  = currentUser.Email ?? string.Empty;

        // Keyed per user; write commands invalidate via the matching tag.
        var result = await cache.GetOrCreateAsync(
            key:     AccountsCacheKeys.UserProfile(userId),
            state:   (profileRepository, securityService, userId, email),
            factory: static async (state, ct) =>
                await BuildResultAsync(
                    state.profileRepository,
                    state.securityService,
                    state.userId,
                    state.email,
                    ct),
            options: _cacheOptions,
            tags:    [AccountsCacheKeys.UserProfileTag(userId)],
            cancellationToken: cancellationToken);

        return result is null
            ? Result<GetProfileResult>.Failure(
                Error.NotFound("Profile", "Profile not found."),
                Outcome.NotFound)
            : Result<GetProfileResult>.Success(result);
    }

    private static async Task<GetProfileResult?> BuildResultAsync(
        IProfileRepository profileRepository,
        ISecurityService securityService,
        Guid userId,
        string email,
        CancellationToken ct)
    {
        // Read-only query — default asNoTracking: true is correct.
        var profile = await profileRepository.FirstOrDefaultAsync(
            filter: p => p.UserId == userId,
            ct:     ct);

        if (profile is null)
            return null;

        var phoneNumber = await securityService.GetPrimaryPhoneNumberAsync(userId, ct);

        return new GetProfileResult(
            UserId:      profile.UserId,
            FirstName:   profile.FirstName,
            LastName:    profile.LastName,
            DisplayName: profile.DisplayName,
            AvatarUrl:   profile.AvatarUrl,
            PhoneNumber: phoneNumber,
            DateOfBirth: profile.DateOfBirth,
            Gender:      profile.Gender?.ToString(),
            Country:     profile.Country,
            City:        profile.City,
            AddressLine: profile.AddressLine,
            Email:       email);
    }
}
