using Accounts.Application.Caching;
using Accounts.Contracts.Abstractions;
using Accounts.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Services;

internal sealed class ProfileReassignmentService(
    IProfileRepository profileRepository,
    IAccountsUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<ProfileReassignmentService> logger)
    : IProfileReassignmentService
{
    public async Task<Result<ProfileReassignmentOutcome>> ResetForReassignmentAsync(
        ProfileReassignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return Result<ProfileReassignmentOutcome>.Failure(
                Error.Validation("Profile.Request", "Reassignment request is required."),
                Outcome.Invalid);
        }

        if (request.UserId == Guid.Empty)
        {
            return Result<ProfileReassignmentOutcome>.Failure(
                Error.Validation("Profile.UserId", "UserId is required."),
                Outcome.Invalid);
        }

        var profile = await profileRepository.FirstOrDefaultAsync(
            filter:       p => p.UserId == request.UserId,
            asNoTracking: false,
            ct:           cancellationToken);

        if (profile is null)
        {
            logger.LogInformation(
                "Accounts: No profile row for user {UserId} — ResetForReassignment is a no-op. " +
                "A subsequent UserCreatedIntegrationEvent replay will create a clean profile.",
                request.UserId);
            return Result<ProfileReassignmentOutcome>.Success(new ProfileReassignmentOutcome(Scrubbed: false));
        }

        var localPart = ExtractLocalPart(request.NewEmail);

        profile.ResetForReassignment(localPart);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Evict the per-user profile cache tag so the next
        // GetProfile read reflects the scrubbed placeholders.
        await cache.RemoveByTagAsync(
            AccountsCacheKeys.UserProfileTag(request.UserId),
            cancellationToken);

        logger.LogInformation(
            "Accounts: Profile {ProfileId} for user {UserId} reset for reassignment. " +
            "DisplayName={DisplayName}.",
            profile.Id,
            request.UserId,
            profile.DisplayName ?? "(null)");

        return Result<ProfileReassignmentOutcome>.Success(new ProfileReassignmentOutcome(Scrubbed: true));
    }

    private static string? ExtractLocalPart(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var atIndex = email.IndexOf('@');
        if (atIndex <= 0)
            return null;

        var localPart = email[..atIndex].Trim();
        return string.IsNullOrEmpty(localPart) ? null : localPart;
    }
}
