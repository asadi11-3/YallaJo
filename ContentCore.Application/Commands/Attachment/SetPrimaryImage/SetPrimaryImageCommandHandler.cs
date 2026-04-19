using ContentCore.Domain.Entities;
using ContentCore.Domain.Enums;
using ContentCore.Domain.Exceptions;
using ContentCore.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Attachment.SetPrimaryImage;

public sealed class SetPrimaryImageCommandHandler(
    IAttachmentRepository attachmentRepository,
    IContentCoreUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<SetPrimaryImageCommandHandler> logger)
    : ICommandHandler<SetPrimaryImageCommand>
{
    public async Task<Result> Handle(SetPrimaryImageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (currentUser.UserId is null)
                return Result.Failure(Error.Unauthorized("Authentication is required."), Outcome.Unauthorized);

            // Load for validation only (ownership + entity check) — no mutation on the attachment itself.
            var attachment = await attachmentRepository.GetByIdAsync(
                request.AttachmentId, cancellationToken, asNoTracking: true);

            if (attachment is null)
            {
                return Result.Failure(
                    new Error("Attachment.NotFound", $"Attachment '{request.AttachmentId}' was not found."),
                    Outcome.NotFound);
            }

            if (attachment.EntityType != request.EntityType || attachment.EntityId != request.EntityId)
            {
                return Result.Failure(
                    new Error(
                        "Attachment.WrongEntity",
                        $"Attachment '{request.AttachmentId}' does not belong to {request.EntityType}/{request.EntityId}."),
                    Outcome.Invalid);
            }

            // IDOR: only the uploader or an admin may set the primary image
            var isAdmin = currentUser.IsInRole("Admin");
            if (!isAdmin && attachment.UploadedByUserId != currentUser.UserId.Value)
            {
                return Result.Failure(
                    Error.Forbidden("You do not have permission to set the primary image for this entity."),
                    Outcome.Forbidden);
            }

            var entityImages = await attachmentRepository.GetEntityImagesAsync(
                request.EntityType, request.EntityId, cancellationToken);

            foreach (var image in entityImages)
                image.SetPrimary(false);

            var target = entityImages.FirstOrDefault(x => x.AttachmentId == request.AttachmentId);
            if (target is not null)
            {
                target.SetPrimary(true);
            }
            else
            {
                var newImage = EntityImage.Create(
                    request.EntityType,
                    request.EntityId,
                    request.AttachmentId,
                    ImageSize.Original,
                    sortOrder: 0,
                    isPrimary: true);
                attachmentRepository.AddEntityImage(newImage);
            }

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (ContentCoreConcurrencyException)
            {
                return Result.Failure(
                    new Error(
                        "Attachment.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync($"attachments:{request.EntityType}:{request.EntityId}", cancellationToken);

            logger.LogInformation(
                "Primary image set: Attachment={AttachmentId} for {EntityType}/{EntityId}",
                request.AttachmentId, request.EntityType, request.EntityId);

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
