using ContentCore.Application.Queries.PromoBlock.Common;
using ContentCore.Domain.Entities;
using ContentCore.Domain.Enums;
using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Storage;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;
// Disambiguate the entity from the sibling command namespace ContentCore.Application.Commands.Attachment.
using AttachmentEntity = ContentCore.Domain.Entities.Attachment;

namespace ContentCore.Application.Commands.PromoBlock.SetPromoBlockImage;

public sealed class SetPromoBlockImageCommandHandler(
    IPromoBlockRepository promoBlockRepository,
    IAttachmentRepository attachmentRepository,
    IFileStorageService fileStorageService,
    IContentCoreUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<SetPromoBlockImageCommandHandler> logger)
    : ICommandHandler<SetPromoBlockImageCommand, SetPromoBlockImageResult>
{
    // Folder mirrors the Attachment pipeline convention: EntityType.ToString().ToLowerInvariant() + "s".
    private const string Folder = "promoblocks";

    public async Task<Result<SetPromoBlockImageResult>> Handle(
        SetPromoBlockImageCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var promoBlock = await promoBlockRepository.GetByPlacementKeyAsync(
                request.PlacementKey,
                cancellationToken);

            if (promoBlock is null)
            {
                return Result<SetPromoBlockImageResult>.Failure(
                    new Error(
                        "PromoBlock.NotFound",
                        $"Promotional placement '{request.PlacementKey}' was not found."),
                    Outcome.NotFound);
            }

            // The upload endpoint is admin-gated (MustHavePermission Promotion.Update), so an
            // authenticated user is guaranteed here. Guard defensively because Attachment.Create
            // rejects an empty uploader id.
            if (currentUser.UserId is not { } uploadedByUserId || uploadedByUserId == Guid.Empty)
            {
                return Result<SetPromoBlockImageResult>.Failure(
                    new Error("PromoBlock.Unauthorized", "An authenticated user is required to upload an image."),
                    Outcome.Unauthorized);
            }

            // Validate the actual bytes, not just the declared content type.
            if (!await IsSupportedImageAsync(request.FileStream, cancellationToken))
            {
                return Result<SetPromoBlockImageResult>.Failure(
                    new Error(
                        "PromoBlock.InvalidImage",
                        "The uploaded file is not a valid PNG, JPEG, or WEBP image."),
                    Outcome.Invalid);
            }

            var uploadResult = await fileStorageService.UploadAsync(
                request.FileStream,
                request.FileName,
                request.ContentType,
                Folder,
                cancellationToken);

            if (uploadResult.IsFailure)
            {
                return Result<SetPromoBlockImageResult>.Failure(
                    uploadResult.Error ?? new Error("PromoBlock.UploadFailed", "Image upload failed."),
                    Outcome.ServerError);
            }

            var previousImageUrl = promoBlock.ImageUrl;
            var previousAttachmentId = promoBlock.AttachmentId;
            var uploadedUrl = uploadResult.Value!.Url;

            // Reuse the existing Attachment/FileAsset pipeline: persist a tracked Attachment row
            // for the uploaded file and reference it from the promo block (decision: PromoBlock is
            // a first-class EntityType that owns its image through AttachmentId).
            var attachment = AttachmentEntity.Create(
                EntityType.PromoBlock,
                promoBlock.Id,
                AttachmentType.Image,
                uploadedUrl,
                uploadedByUserId,
                originalFileName: request.FileName,
                mimeType: request.ContentType,
                fileSize: uploadResult.Value!.FileSize,
                sortOrder: 0);

            await attachmentRepository.AddAsync(attachment, cancellationToken);

            // Store BOTH the resolved image url (fast-path/fallback rendering) and the attachment id.
            promoBlock.SetImage(uploadedUrl, attachment.Id);

            // If an older attachment backed this placement, mark it for cleanup in the same unit of work.
            if (previousAttachmentId is { } oldAttachmentId && oldAttachmentId != attachment.Id)
            {
                var previousAttachment = await attachmentRepository.GetByIdAsync(
                    oldAttachmentId,
                    cancellationToken,
                    asNoTracking: false);
                previousAttachment?.MarkForDeletion();
            }

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Roll back the just-uploaded file so we don't orphan it.
                await TryDeleteAsync(uploadedUrl, cancellationToken);
                return Result<SetPromoBlockImageResult>.Conflict(
                    new Error(
                        "PromoBlock.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."));
            }

            // Best-effort cleanup of the replaced physical file (ignore failures).
            if (!string.IsNullOrWhiteSpace(previousImageUrl) &&
                !string.Equals(previousImageUrl, uploadedUrl, StringComparison.Ordinal))
            {
                await TryDeleteAsync(previousImageUrl!, cancellationToken);
            }

            logger.LogInformation(
                "Promo block image updated: {PlacementKey} -> {ImageUrl} (attachment {AttachmentId})",
                promoBlock.PlacementKey,
                uploadedUrl,
                attachment.Id);

            return Result<SetPromoBlockImageResult>.Success(
                new SetPromoBlockImageResult(PromoBlockDto.From(promoBlock)));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<SetPromoBlockImageResult>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }

    private async Task TryDeleteAsync(string url, CancellationToken ct)
    {
        try
        {
            await fileStorageService.DeleteAsync(url, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to clean up promo block image {ImageUrl}", url);
        }
    }

    /// <summary>
    /// Verifies the stream begins with a PNG, JPEG, or WEBP magic-byte signature,
    /// then rewinds it for the subsequent upload.
    /// </summary>
    private static async Task<bool> IsSupportedImageAsync(Stream stream, CancellationToken ct)
    {
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        var header = new byte[12];
        var read = await ReadExactlyAsync(stream, header, ct);

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        if (read < 12)
        {
            return false;
        }

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
            header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
        {
            return true;
        }

        // JPEG: FF D8 FF
        if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return true;
        }

        // WEBP: "RIFF" .... "WEBP"
        if (header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
            header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)
        {
            return true;
        }

        return false;
    }

    private static async Task<int> ReadExactlyAsync(Stream stream, byte[] buffer, CancellationToken ct)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(total, buffer.Length - total), ct);
            if (read == 0)
            {
                break;
            }

            total += read;
        }

        return total;
    }
}
