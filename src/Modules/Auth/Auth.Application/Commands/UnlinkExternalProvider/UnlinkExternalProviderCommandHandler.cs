using Auth.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.UnlinkExternalProvider;

public sealed class UnlinkExternalProviderCommandHandler(
    IExternalProviderRepository externalProviderRepository,
    IAuthUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ISecurityService securityService)
    : ICommandHandler<UnlinkExternalProviderCommand>
{
    public async Task<Result> Handle(UnlinkExternalProviderCommand request, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result.Unauthorized("Authentication is required.");

        var externalProvider = await externalProviderRepository.GetByIdAsync(
            request.ExternalProviderId,
            ct: ct,
            asNoTracking: false);

        if (externalProvider is null)
            return Result.NotFound($"External provider link {request.ExternalProviderId} was not found.");

        if (externalProvider.UserId != currentUser.UserId.Value)
            return Result.Forbidden("You are not authorized to unlink this external provider.");

        if (externalProvider.IsActive)
        {
            // B6 — lockout guard: refuse to remove the user's LAST usable sign-in method.
            // Applies only while this link is still active (re-unlinking a deactivated
            // link stays idempotent). Blocks when this is the only remaining active link
            // AND the account has no usable local password (external-only registrations
            // carry the non-Base64 "EXTERNAL-ONLY:<guid>" placeholder — the cross-module
            // ISecurityService.HasUsablePasswordAsync check mirrors the existing
            // Login-handler pattern of consuming Security.Contracts from Auth).
            var activeLinks = await externalProviderRepository.GetAllAsync(
                filter: p => p.UserId == currentUser.UserId.Value && p.IsActive,
                asNoTracking: true,
                ct: ct);

            if (activeLinks.Count <= 1
                && !await securityService.HasUsablePasswordAsync(currentUser.UserId.Value, ct))
            {
                return Result.Failure(
                    new Error(
                        "ExternalProvider.LastLoginMethod",
                        "This is your only way to sign in. Set a password first, then unlink this account."),
                    Outcome.Conflict);
            }

            externalProvider.Deactivate();
        }

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(
                new Error("ExternalProvider.ConcurrencyConflict", "The external provider link was modified concurrently. Reload and retry."),
                Outcome.Conflict);
        }

        return Result.Success();
    }
}
