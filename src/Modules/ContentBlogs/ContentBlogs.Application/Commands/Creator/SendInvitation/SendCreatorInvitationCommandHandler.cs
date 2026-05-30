using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Entities.Creators;
using ContentBlogs.Domain.Enums;
using ContentBlogs.Domain.Errors;
using ContentBlogs.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Commands.Creator.SendInvitation;

public sealed class SendCreatorInvitationCommandHandler(
    ICreatorInvitationRepository invitationRepository,
    IContentBlogsUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<SendCreatorInvitationCommandHandler> logger)
    : ICommandHandler<SendCreatorInvitationCommand, SendCreatorInvitationResult>
{
    public async Task<Result<SendCreatorInvitationResult>> Handle(
        SendCreatorInvitationCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
                        var adminId = currentUser.UserId!.Value;

            // ── Guard: no duplicate pending invitation ──────────────────────
            if (request.Kind == CreatorInvitationKind.Email && !string.IsNullOrEmpty(request.Email))
            {
                if (await invitationRepository
                    .HasPendingInvitationForEmailAsync(request.Email, cancellationToken)
                    .ConfigureAwait(false))
                {
                    return Result.Failure<SendCreatorInvitationResult>(
                        new Error("Creator.DuplicateInvitation",
                            "A pending invitation already exists for this email."),
                        Outcome.Conflict);
                }
            }

            if (request.Kind == CreatorInvitationKind.InApp && request.InvitedUserId.HasValue)
            {
                if (await invitationRepository
                    .HasPendingInvitationForUserAsync(request.InvitedUserId.Value, cancellationToken)
                    .ConfigureAwait(false))
                {
                    return Result.Failure<SendCreatorInvitationResult>(
                        new Error("Creator.DuplicateInvitation",
                            "A pending invitation already exists for this user."),
                        Outcome.Conflict);
                }
            }

            // ── Create invitation ───────────────────────────────────────────
            var invitationResult = request.Kind == CreatorInvitationKind.Email
                ? CreatorInvitation.CreateEmail(request.Email!, adminId, request.PersonalMessage)
                : CreatorInvitation.CreateInApp(request.InvitedUserId!.Value, adminId, request.PersonalMessage);

            if (invitationResult.IsFailure)
            {
                return Result.Failure<SendCreatorInvitationResult>(
                    invitationResult.Errors.FirstOrDefault()!, Outcome.UnprocessableEntity);
            }

            await invitationRepository
                .AddAsync(invitationResult.Value, cancellationToken)
                .ConfigureAwait(false);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure<SendCreatorInvitationResult>(
                    new Error("Creator.ConcurrencyConflict",
                        "Invitation was modified concurrently."),
                    Outcome.Conflict);
            }

            logger.LogInformation(
                "CreatorInvitation sent: {InvitationId} (Kind={Kind}, AdminId={AdminId})",
                invitationResult.Value.Id, request.Kind, adminId);

            return Result.Created(new SendCreatorInvitationResult(
                invitationResult.Value.Id, invitationResult.Value.Token));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<SendCreatorInvitationResult>(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
