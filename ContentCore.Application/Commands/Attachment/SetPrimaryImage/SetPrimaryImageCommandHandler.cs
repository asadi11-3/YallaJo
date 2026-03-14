using ContentCore.Application.Caching;
using ContentCore.Domain.Entities;
using ContentCore.Domain.Enums;
using ContentCore.Domain.Repositories;
using Microsoft.Extensions.Caching.Memory;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Commands.Attachment.SetPrimaryImage;

public sealed class SetPrimaryImageCommandHandler(
    IAttachmentRepository attachmentRepository,
    IContentCoreUnitOfWork unitOfWork,
    IMemoryCache cache)
    : ICommandHandler<SetPrimaryImageCommand>
{
    public async Task<Result> Handle(SetPrimaryImageCommand request, CancellationToken ct)
    {
        // 1. Verify attachment exists
        var attachment = await attachmentRepository.GetByIdAsync(request.AttachmentId, ct);
        if (attachment is null)
            return Result.NotFound($"Attachment '{request.AttachmentId}' not found.");

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

        await unitOfWork.SaveChangesAsync(ct);
        cache.Remove(ContentCoreCacheKeys.EntityAttachments(request.EntityType.ToString(), request.EntityId));

        return Result.Success();
    }
}