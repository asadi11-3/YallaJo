using ContentCore.Domain.Entities;
using ContentCore.Domain.Enums;
using ContentCore.Domain.Events;
using ContentCore.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Infrastructure.EventHandlers;

/// <summary>
/// Handles AttachmentUploadedDomainEvent:
///   1. Auto-creates an EntityImage record when the attachment is an image.
///   2. Logs the upload for auditing.
/// Thumbnail generation and media metadata extraction are deferred to
/// Cloudinary (or similar) integration later.
/// </summary>
public sealed class AttachmentUploadedDomainEventHandler(
    IAttachmentRepository attachmentRepository,
    ILogger<AttachmentUploadedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<AttachmentUploadedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<AttachmentUploadedDomainEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;

        logger.LogInformation(
            "AttachmentUploadedDomainEvent: Attachment {AttachmentId} uploaded for {EntityType}/{EntityId} (Type: {AttachmentType}).",
            evt.AttachmentId, evt.EntityType, evt.EntityId, evt.AttachmentType);

        // Auto-create EntityImage record for image attachments
        if (evt.AttachmentType == AttachmentType.Image)
        {
            var existingImages = await attachmentRepository.GetEntityImagesAsync(
                evt.EntityType, evt.EntityId, ct);

            // First image for this entity becomes primary automatically
            var isPrimary = existingImages.Count == 0;

            var entityImage = EntityImage.Create(
                evt.EntityType,
                evt.EntityId,
                evt.AttachmentId,
                ImageSize.Original,
                sortOrder: existingImages.Count,
                isPrimary: isPrimary);

            attachmentRepository.AddEntityImage(entityImage);

            logger.LogInformation(
                "AttachmentUploadedDomainEvent: Created EntityImage for attachment {AttachmentId} (IsPrimary: {IsPrimary}).",
                evt.AttachmentId, isPrimary);
        }
    }
}