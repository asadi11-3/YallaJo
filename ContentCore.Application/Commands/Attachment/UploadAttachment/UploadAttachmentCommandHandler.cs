using ContentCore.Application.Authorization;
using ContentCore.Application.Caching;
using ContentCore.Application.Interfaces;
using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Storage;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Attachment.UploadAttachment;

public sealed class UploadAttachmentCommandHandler(
    IFileStorageService fileStorageService,
    IAttachmentRepository attachmentRepository,
    IContentCoreUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IEntityOwnershipResolver ownershipResolver,
    IMediaProcessingQueue mediaProcessingQueue,
    HybridCache cache,
    ILogger<UploadAttachmentCommandHandler> logger)
    : ICommandHandler<UploadAttachmentCommand, UploadAttachmentResult>
{
    private const int SignatureProbeLength = 32;

    private enum DetectedFileType
    {
        Unknown = 0,
        Jpeg,
        Png,
        Gif,
        Webp,
        Mp4,
        Mov,
        Webm,
        Avi,
        Mp3,
        Ogg,
        Wav,
        Pdf,
    }

    public async Task<Result<UploadAttachmentResult>> Handle(
        UploadAttachmentCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (currentUser.UserId is null)
            {
                return Result<UploadAttachmentResult>.Failure(
                    Error.Unauthorized("Authentication is required."),
                    Outcome.Unauthorized);
            }

            // IDOR / spoofing guard: only the current user (or an admin-tier role: Admin/SuperAdmin/Owner)
            // may upload as UploadedByUserId.
            var isAdminTier = AppRoles.HighestPrivilegeLevel(currentUser.Roles)
                >= RolePrivilegeLevel.Admin;

            if (!isAdminTier && request.UploadedByUserId != currentUser.UserId.Value)
            {
                return Result<UploadAttachmentResult>.Failure(
                    Error.Forbidden("You cannot upload attachments on behalf of another user."),
                    Outcome.Forbidden);
            }

            // Target-entity ownership probe (skipped for admin-tier callers).
            // Runs BEFORE file validation, storage upload, DB write, cache eviction,
            // and media-queue enqueue. A non-admin caller must own the target entity.
            if (!isAdminTier)
            {
                var ownership = await ownershipResolver.ResolveAsync(
                    request.EntityType, request.EntityId, cancellationToken);

                if (!ownership.IsSupported)
                {
                    return Result<UploadAttachmentResult>.Failure(
                        new Error(
                            "Attachment.UnsupportedEntityType",
                            "This entity type cannot be authorized for attachment operations."),
                        Outcome.Invalid);
                }

                if (!ownership.Exists)
                {
                    return Result<UploadAttachmentResult>.Failure(
                        new Error(
                            "Attachment.TargetNotFound",
                            $"{request.EntityType} '{request.EntityId}' was not found."),
                        Outcome.NotFound);
                }

                if (ownership.IsDeleted)
                {
                    return Result<UploadAttachmentResult>.Failure(
                        new Error(
                            "Attachment.TargetDeleted",
                            $"{request.EntityType} '{request.EntityId}' is deleted."),
                        Outcome.Invalid);
                }

                if (ownership.OwnerUserId != currentUser.UserId.Value)
                {
                    return Result<UploadAttachmentResult>.Failure(
                        Error.Forbidden("You do not have permission to upload attachments to this entity."),
                        Outcome.Forbidden);
                }
            }

            var detectedFileType = await DetectFileTypeAsync(request.FileStream, cancellationToken);
            if (detectedFileType == DetectedFileType.Unknown)
            {
                return Result<UploadAttachmentResult>.Failure(
                    new Error(
                        "Attachment.InvalidSignature",
                        "The file signature is missing, unsupported, or does not match an allowed media/document format."),
                    Outcome.Invalid);
            }

            if (!IsAttachmentTypeCompatible(detectedFileType, request.Type))
            {
                return Result<UploadAttachmentResult>.Failure(
                    new Error(
                        "Attachment.TypeMismatch",
                        $"The uploaded file signature '{detectedFileType}' is not compatible with attachment type '{request.Type}'."),
                    Outcome.Invalid);
            }

            if (!IsMimeTypeCompatible(detectedFileType, request.ContentType))
            {
                return Result<UploadAttachmentResult>.Failure(
                    new Error(
                        "Attachment.ContentTypeMismatch",
                        $"The uploaded file signature '{detectedFileType}' does not match content type '{request.ContentType}'."),
                    Outcome.Invalid);
            }

            if (!IsFileExtensionCompatible(detectedFileType, request.FileName))
            {
                return Result<UploadAttachmentResult>.Failure(
                    new Error(
                        "Attachment.ExtensionMismatch",
                        $"The uploaded file signature '{detectedFileType}' does not match file extension '{Path.GetExtension(request.FileName)}'."),
                    Outcome.Invalid);
            }

            // 1. Upload physical file to storage first.
            // If DB save fails below we will attempt to clean up the orphaned file.
            var folder = request.EntityType.ToString().ToLowerInvariant() + "s";
            var uploadResult = await fileStorageService.UploadAsync(
                request.FileStream,
                request.FileName,
                request.ContentType,
                folder,
                cancellationToken);

            // 2. Create domain entity with uploaded file metadata
            var attachment = Domain.Entities.Attachment.Create(
                request.EntityType,
                request.EntityId,
                request.Type,
                uploadResult.Url,
                request.UploadedByUserId,
                request.FileName,
                request.ContentType,
                uploadResult.FileSize,
                request.SortOrder);

            // 3. Set optional media metadata
            if (request.Width.HasValue && request.Height.HasValue)
                attachment.SetDimensions(request.Width.Value, request.Height.Value);

            if (request.DurationSeconds.HasValue)
                attachment.SetDuration(request.DurationSeconds.Value);

            await attachmentRepository.AddAsync(attachment, cancellationToken);

            // 4. Persist DB record. On failure, best-effort clean up the uploaded physical file
            // to avoid orphaning it (no DB record will ever point to it again).
            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                await TryCleanupOrphanedFileAsync(uploadResult.Url, cancellationToken);
                return Result<UploadAttachmentResult>.Conflict(
                    new Error(
                        "Attachment.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await TryCleanupOrphanedFileAsync(uploadResult.Url, cancellationToken);
                throw; // re-throw so the outer handler reports it correctly
            }

            await cache.RemoveByTagAsync(
                ContentCoreCacheKeys.EntityAttachmentsTag(request.EntityType.ToString(), request.EntityId),
                cancellationToken);

            // 5. Enqueue background media processing AFTER confirmed DB save.
            await mediaProcessingQueue.EnqueueAsync(
                new MediaProcessingJob(attachment.Id, attachment.Url, request.Type), cancellationToken);

            logger.LogInformation(
                "Attachment uploaded: {AttachmentId} (EntityType={EntityType}, EntityId={EntityId}, Size={Size})",
                attachment.Id, request.EntityType, request.EntityId, uploadResult.FileSize);

            return Result<UploadAttachmentResult>.Created(
                new UploadAttachmentResult(attachment.Id, attachment.Url, uploadResult.FileSize));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<UploadAttachmentResult>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }

    private async Task TryCleanupOrphanedFileAsync(string fileUrl, CancellationToken ct)
    {
        try
        {
            await fileStorageService.DeleteAsync(fileUrl, ct);
            logger.LogWarning(
                "DB save failed after upload — cleaned up orphaned file: {FileUrl}", fileUrl);
        }
        catch (Exception cleanupEx)
        {
            logger.LogError(
                cleanupEx,
                "DB save failed after upload AND cleanup failed — orphaned file may need manual removal: {FileUrl}",
                fileUrl);
        }
    }

    private static async Task<DetectedFileType> DetectFileTypeAsync(Stream stream, CancellationToken ct)
    {
        if (!stream.CanRead || !stream.CanSeek)
            return DetectedFileType.Unknown;

        var originalPosition = stream.Position;
        var buffer = new byte[SignatureProbeLength];

        try
        {
            stream.Seek(0, SeekOrigin.Begin);
            var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, SignatureProbeLength), ct);
            if (bytesRead <= 0)
                return DetectedFileType.Unknown;

            return DetectFileType(buffer, bytesRead);
        }
        finally
        {
            stream.Seek(originalPosition, SeekOrigin.Begin);
        }
    }

    private static DetectedFileType DetectFileType(byte[] buffer, int bytesRead)
    {
        if (HasPrefix(buffer, bytesRead, 0xFF, 0xD8, 0xFF))
            return DetectedFileType.Jpeg;

        if (HasPrefix(buffer, bytesRead, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A))
            return DetectedFileType.Png;

        if (HasAscii(buffer, bytesRead, 0, "GIF87a") || HasAscii(buffer, bytesRead, 0, "GIF89a"))
            return DetectedFileType.Gif;

        if (HasAscii(buffer, bytesRead, 0, "RIFF") && HasAscii(buffer, bytesRead, 8, "WEBP"))
            return DetectedFileType.Webp;

        if (HasAscii(buffer, bytesRead, 4, "ftyp"))
        {
            if (HasAscii(buffer, bytesRead, 8, "qt  "))
                return DetectedFileType.Mov;

            return DetectedFileType.Mp4;
        }

        if (HasPrefix(buffer, bytesRead, 0x1A, 0x45, 0xDF, 0xA3))
            return DetectedFileType.Webm;

        if (HasAscii(buffer, bytesRead, 0, "RIFF") && HasAscii(buffer, bytesRead, 8, "AVI "))
            return DetectedFileType.Avi;

        if (HasAscii(buffer, bytesRead, 0, "OggS"))
            return DetectedFileType.Ogg;

        if (HasAscii(buffer, bytesRead, 0, "RIFF") && HasAscii(buffer, bytesRead, 8, "WAVE"))
            return DetectedFileType.Wav;

        if (HasAscii(buffer, bytesRead, 0, "%PDF"))
            return DetectedFileType.Pdf;

        if (HasAscii(buffer, bytesRead, 0, "ID3") ||
            (bytesRead >= 2 && buffer[0] == 0xFF && (buffer[1] & 0xE0) == 0xE0))
            return DetectedFileType.Mp3;

        return DetectedFileType.Unknown;
    }

    private static bool IsAttachmentTypeCompatible(DetectedFileType detected, Domain.Enums.AttachmentType requestedType)
        => detected switch
        {
            DetectedFileType.Jpeg or DetectedFileType.Png or DetectedFileType.Gif or DetectedFileType.Webp
                => requestedType == Domain.Enums.AttachmentType.Image,

            DetectedFileType.Mp4 or DetectedFileType.Mov or DetectedFileType.Avi
                => requestedType == Domain.Enums.AttachmentType.Video,

            DetectedFileType.Webm
                => requestedType is Domain.Enums.AttachmentType.Video or Domain.Enums.AttachmentType.Audio,

            DetectedFileType.Mp3 or DetectedFileType.Ogg or DetectedFileType.Wav
                => requestedType == Domain.Enums.AttachmentType.Audio,

            DetectedFileType.Pdf
                => requestedType == Domain.Enums.AttachmentType.Document,

            _ => false,
        };

    private static bool IsMimeTypeCompatible(DetectedFileType detected, string contentType)
    {
        var normalizedContentType = NormalizeContentType(contentType);

        return detected switch
        {
            DetectedFileType.Jpeg => normalizedContentType == "image/jpeg",
            DetectedFileType.Png => normalizedContentType == "image/png",
            DetectedFileType.Gif => normalizedContentType == "image/gif",
            DetectedFileType.Webp => normalizedContentType == "image/webp",
            DetectedFileType.Mp4 => normalizedContentType == "video/mp4",
            DetectedFileType.Mov => normalizedContentType == "video/quicktime",
            DetectedFileType.Webm => normalizedContentType is "video/webm" or "audio/webm",
            DetectedFileType.Avi => normalizedContentType == "video/x-msvideo",
            DetectedFileType.Mp3 => normalizedContentType == "audio/mpeg",
            DetectedFileType.Ogg => normalizedContentType == "audio/ogg",
            DetectedFileType.Wav => normalizedContentType == "audio/wav",
            DetectedFileType.Pdf => normalizedContentType == "application/pdf",
            _ => false,
        };
    }

    private static bool IsFileExtensionCompatible(DetectedFileType detected, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return detected switch
        {
            DetectedFileType.Jpeg => extension is ".jpg" or ".jpeg",
            DetectedFileType.Png => extension == ".png",
            DetectedFileType.Gif => extension == ".gif",
            DetectedFileType.Webp => extension == ".webp",
            DetectedFileType.Mp4 => extension == ".mp4",
            DetectedFileType.Mov => extension == ".mov",
            DetectedFileType.Webm => extension == ".webm",
            DetectedFileType.Avi => extension == ".avi",
            DetectedFileType.Mp3 => extension == ".mp3",
            DetectedFileType.Ogg => extension == ".ogg",
            DetectedFileType.Wav => extension == ".wav",
            DetectedFileType.Pdf => extension == ".pdf",
            _ => false,
        };
    }

    private static string NormalizeContentType(string contentType)
    {
        var separatorIndex = contentType.IndexOf(';');
        var mime = separatorIndex >= 0
            ? contentType[..separatorIndex]
            : contentType;

        return mime.Trim().ToLowerInvariant();
    }

    private static bool HasPrefix(byte[] buffer, int bytesRead, params byte[] signature)
    {
        if (bytesRead < signature.Length)
            return false;

        for (var i = 0; i < signature.Length; i++)
        {
            if (buffer[i] != signature[i])
                return false;
        }

        return true;
    }

    private static bool HasAscii(byte[] buffer, int bytesRead, int offset, string text)
    {
        if (offset < 0)
            return false;

        if (bytesRead < offset + text.Length)
            return false;

        for (var i = 0; i < text.Length; i++)
        {
            if (buffer[offset + i] != text[i])
                return false;
        }

        return true;
    }
}
