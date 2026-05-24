using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Errors;
using ContentBlogs.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Commands.Creator.RedeemInvitation;

public sealed class RedeemCreatorInvitationCommandHandler(
    ICreatorInvitationRepository invitationRepository,
    ICreatorApplicationRepository applicationRepository,
    IContentBlogsUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<RedeemCreatorInvitationCommandHandler> logger)
    : ICommandHandler<RedeemCreatorInvitationCommand>
{
    public async Task<Result> Handle(
        RedeemCreatorInvitationCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
                        var userId = currentUser.UserId!.Value;

            var invitation = await invitationRepository
                .GetByTokenAsync(request.Token, cancellationToken)
                .ConfigureAwait(false);

            if (invitation is null)
            {
                return Result.Failure(
                    CreatorInvitationErrors.NotFound, Outcome.NotFound);
            }

            // ── Guard: user doesn't already have an active application ─────
            if (await applicationRepository
                .HasActiveApplicationAsync(userId, cancellationToken)
                .ConfigureAwait(false))
            {
                return Result.Failure(
                    CreatorApplicationErrors.AlreadyHasActiveApplication,
                    Outcome.Conflict);
            }

            var redeemResult = invitation.Redeem(userId);
            if (redeemResult.IsFailure)
            {
                return Result.Failure(redeemResult.Errors.FirstOrDefault()!, Outcome.UnprocessableEntity);
            }

            invitationRepository.Update(invitation);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(
                    new Error("Creator.ConcurrencyConflict",
                        "Invitation was modified concurrently."),
                    Outcome.Conflict);
            }

            logger.LogInformation(
                "CreatorInvitation redeemed: {InvitationId} by user {UserId}",
                invitation.Id, userId);

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
