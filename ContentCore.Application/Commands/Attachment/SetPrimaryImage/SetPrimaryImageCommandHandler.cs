using ContentCore.Domain.Entities;
using ContentCore.Domain.Enums;
using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Attachment.SetPrimaryImage;

public sealed class SetPrimaryImageCommandHandler(
    IAttachmentRepository attachmentRepository,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache)
    : ICommandHandler<SetPrimaryImageCommand>
{
    public async Task<Result> Handle(SetPrimaryImageCommand request, CancellationToken ct)
    {
        try
        {
            // 1. Verify attachment exists
            var attachment = await attachmentRepository.GetByIdAsync(request.AttachmentId, ct);
            if (attachment is null)
                return Result.Failure(
                    new Error("Attachment.NotFound", $"Attachment '{request.AttachmentId}' was not found."),
                    Outcome.NotFound);

            // 2. Get all EntityImages for this entity
            var entityImages = await attachmentRepository.GetEntityImagesAsync(
                request.EntityType, request.EntityId, ct);

            // 3. Clear existing primary
            foreach (var image in entityImages)
                image.SetPrimary(false);

            // 4. Set target as primary (or create if it doesn't exist)
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
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(
                    new Error("Attachment.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync($"attachments:{request.EntityType}:{request.EntityId}", ct);

            return Result.Success();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
