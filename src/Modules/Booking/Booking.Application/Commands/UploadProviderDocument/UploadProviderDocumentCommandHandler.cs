using Booking.Application.Caching;
using Booking.Application.Commands.Common;
using Booking.Application.Interfaces;
using Booking.Domain.Entities;
using Booking.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Storage;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.Exceptions;

namespace Booking.Application.Commands.UploadProviderDocument;

public sealed class UploadProviderDocumentCommandHandler(
    IProviderDocumentRepository documentRepository,
    IFileStorageService fileStorageService,
    IBookingUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<UploadProviderDocumentCommandHandler> logger)
    : ICommandHandler<UploadProviderDocumentCommand, ProviderDocumentDto>
{
    private const string StorageFolder = "provider-documents";

    public async Task<Result<ProviderDocumentDto>> Handle(
        UploadProviderDocumentCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var ownership = await ProviderDocumentOwnership
                .ResolveTourGuideIdAsync(currentUser, documentRepository, cancellationToken)
                .ConfigureAwait(false);

            if (!ownership.IsSuccess)
            {
                return Result.Failure<ProviderDocumentDto>(ownership.Errors[0], ownership.Outcome);
            }

            var tourGuideId = ownership.Value;

            var uploadStream = request.FileStream;
            MemoryStream? bufferedStream = null;
            try
            {
                if (!uploadStream.CanSeek)
                {
                    bufferedStream = new MemoryStream();
                    await uploadStream.CopyToAsync(bufferedStream, cancellationToken).ConfigureAwait(false);
                    bufferedStream.Position = 0;
                    uploadStream = bufferedStream;
                }

                var detection = await ProviderDocumentFileValidator
                    .DetectAsync(uploadStream, request.ContentType, request.FileName, cancellationToken)
                    .ConfigureAwait(false);

                if (!detection.IsAcceptable)
                {
                    return Result.Failure<ProviderDocumentDto>(
                        new Error(
                            "ProviderDocument.UnsupportedType",
                            "File signature, content type, or extension does not match an allowed type (PDF, JPEG, PNG)."),
                        Outcome.Invalid);
                }

                var duplicate = await documentRepository
                    .ExistsActiveTypeForTourGuideAsync(tourGuideId, request.Type, excludeId: null, cancellationToken)
                    .ConfigureAwait(false);

                if (duplicate)
                {
                    return Result.Failure<ProviderDocumentDto>(
                        new Error(
                            "ProviderDocument.DuplicateType",
                            "Provider already has an active document of this type."),
                        Outcome.Conflict);
                }

                var uploadResponse = await fileStorageService
                    .UploadAsync(uploadStream, request.FileName, request.ContentType, StorageFolder, cancellationToken)
                    .ConfigureAwait(false);

                if (uploadResponse.IsFailure)
                {
                    return Result.Failure<ProviderDocumentDto>(
                        uploadResponse.Errors.Count > 0
                            ? uploadResponse.Errors[0]
                            : new Error("ProviderDocument.UploadFailed", "Failed to upload provider document."),
                        uploadResponse.Outcome);
                }

                var uploadResult = uploadResponse.Value!;

                ProviderDocument document;
                try
                {
                    document = ProviderDocument.CreateForTourGuide(
                        tourGuideId: tourGuideId,
                        documentType: request.Type,
                        documentUrl: uploadResult.Url,
                        originalFileName: request.FileName,
                        expiresAtUtc: request.ExpiresAt?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
                }
                catch (BusinessRuleViolationException ex)
                {
                    await TryCleanupOrphanedFileAsync(uploadResult.Url, cancellationToken).ConfigureAwait(false);
                    return Result.Failure<ProviderDocumentDto>(
                        new Error("ProviderDocument.Invalid", ex.Message),
                        Outcome.Invalid);
                }

                await documentRepository.AddAsync(document, cancellationToken).ConfigureAwait(false);

                try
                {
                    await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (DbUpdateException ex)
                {
                    await TryCleanupOrphanedFileAsync(uploadResult.Url, cancellationToken).ConfigureAwait(false);
                    return Result.Failure<ProviderDocumentDto>(
                        new Error(
                            "ProviderDocument.DuplicateType",
                            "Provider already has an active document of this type."),
                        Outcome.Conflict);
                }
                catch (Exception)
                {
                    await TryCleanupOrphanedFileAsync(uploadResult.Url, cancellationToken).ConfigureAwait(false);
                    throw;
                }

                await cache
                    .RemoveByTagAsync(BookingProviderDocumentCacheKeys.ProviderTag(tourGuideId), cancellationToken)
                    .ConfigureAwait(false);
                await cache
                    .RemoveByTagAsync(BookingProviderDocumentCacheKeys.UserTag(currentUser.UserId!.Value), cancellationToken)
                    .ConfigureAwait(false);

                logger.LogInformation(
                    "Provider document {DocumentId} uploaded by user {UserId} for tour guide {TourGuideId} (type={Type})",
                    document.Id,
                    currentUser.UserId,
                    tourGuideId,
                    document.DocumentType);

                return Result<ProviderDocumentDto>.Created(ToDto(document));
            }
            finally
            {
                bufferedStream?.Dispose();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<ProviderDocumentDto>(
                new Error("Request.Cancelled", "The request was cancelled by the client."),
                Outcome.Canceled);
        }
    }

    private async Task TryCleanupOrphanedFileAsync(string fileUrl, CancellationToken ct)
    {
        try
        {
            await fileStorageService.DeleteAsync(fileUrl, ct).ConfigureAwait(false);
            logger.LogWarning(
                "DB save failed after provider document upload — cleaned up orphaned file: {FileUrl}", fileUrl);
        }
        catch (Exception cleanupEx)
        {
            logger.LogError(
                cleanupEx,
                "DB save failed AND cleanup failed — orphaned file may need manual removal: {FileUrl}",
                fileUrl);
        }
    }

    internal static ProviderDocumentDto ToDto(ProviderDocument document)
        => new(
            Id: document.Id,
            TourGuideId: document.TourGuideId,
            BusinessId: document.BusinessId,
            Type: document.DocumentType,
            FileName: document.OriginalFileName,
            ExpiresAt: document.ExpiresAt,
            Status: document.Status,
            ReviewedAt: document.ReviewedAt,
            RejectionReason: document.RejectionReason,
            CreatedAt: document.CreatedAt,
            RowVersion: Convert.ToBase64String(document.RowVersion ?? []));
}
