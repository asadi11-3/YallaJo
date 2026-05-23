using Booking.Application.Caching;
using Booking.Application.Commands.Common;
using Booking.Application.Commands.UploadProviderDocument;
using Booking.Application.Interfaces;
using Booking.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Storage;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.Exceptions;

namespace Booking.Application.Commands.UpdateProviderDocument;
public sealed class UpdateProviderDocumentCommandHandler(
    IProviderDocumentRepository documentRepository,
    IFileStorageService fileStorageService,
    IBookingUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<UpdateProviderDocumentCommandHandler> logger)
    : ICommandHandler<UpdateProviderDocumentCommand, ProviderDocumentDto>
{
    private const string StorageFolder = "provider-documents";

    public async Task<Result<ProviderDocumentDto>> Handle(
        UpdateProviderDocumentCommand request,
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

            var document = await documentRepository
                .GetByIdTrackedAsync(request.Id, cancellationToken)
                .ConfigureAwait(false);

            if (document is null || document.TourGuideId != tourGuideId)
            {
                return Result.Failure<ProviderDocumentDto>(
                    new Error("ProviderDocument.NotFound", "Provider document was not found."),
                    Outcome.NotFound);
            }

            string? oldUrlToCleanup = null;
            if (request.FileStream is not null)
            {
                var fileResult = await ReplaceFileAsync(document, request, cancellationToken).ConfigureAwait(false);
                if (!fileResult.IsSuccess)
                {
                    return Result.Failure<ProviderDocumentDto>(fileResult.Errors[0], fileResult.Outcome);
                }

                oldUrlToCleanup = fileResult.Value;
            }

            if (request.ExpiresAt.HasValue)
            {
                try
                {
                    document.UpdateExpiry(request.ExpiresAt.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
                }
                catch (BusinessRuleViolationException ex)
                {
                    return Result.Failure<ProviderDocumentDto>(
                        new Error("ProviderDocument.Invalid", ex.Message),
                        Outcome.Invalid);
                }
            }

            documentRepository.AttachAndMarkModifiedWithConcurrency(
                document,
                concurrencyPropertyName: nameof(document.RowVersion),
                originalConcurrencyValue: request.RowVersion);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogInformation(ex, "Provider document {DocumentId} update lost concurrency race", request.Id);
                if (oldUrlToCleanup is not null)
                {
                    await TryCleanupOrphanedFileAsync(oldUrlToCleanup, cancellationToken).ConfigureAwait(false);
                }

                return Result.Failure<ProviderDocumentDto>(
                    new Error(
                        "ProviderDocument.StaleRowVersion",
                        "Document was modified by another caller. Reload and retry."),
                    Outcome.Conflict);
            }
            catch (DbUpdateException ex)
            {
                logger.LogWarning(ex, "Provider document {DocumentId} update failed at SaveChanges", request.Id);
                if (oldUrlToCleanup is not null)
                {
                    await TryCleanupOrphanedFileAsync(oldUrlToCleanup, cancellationToken).ConfigureAwait(false);
                }

                return Result.Failure<ProviderDocumentDto>(
                    new Error("ProviderDocument.PersistFailed", "Could not persist the update; please retry."),
                    Outcome.Conflict);
            }

            if (oldUrlToCleanup is not null)
            {
                await TryCleanupOrphanedFileAsync(oldUrlToCleanup, cancellationToken).ConfigureAwait(false);
            }

            await cache
                .RemoveByTagAsync(BookingProviderDocumentCacheKeys.ProviderTag(tourGuideId), cancellationToken)
                .ConfigureAwait(false);
            await cache
                .RemoveByTagAsync(BookingProviderDocumentCacheKeys.UserTag(currentUser.UserId!.Value), cancellationToken)
                .ConfigureAwait(false);
            await cache
                .RemoveByTagAsync(BookingProviderDocumentCacheKeys.DocumentTag(document.Id), cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "Provider document {DocumentId} updated by user {UserId} (FileReplaced={FileReplaced}, ExpiryUpdated={ExpiryUpdated})",
                document.Id,
                currentUser.UserId,
                request.FileStream is not null,
                request.ExpiresAt.HasValue);

            return Result.Success(UploadProviderDocumentCommandHandler.ToDto(document));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<ProviderDocumentDto>(
                new Error("Request.Cancelled", "The request was cancelled by the client."),
                Outcome.Canceled);
        }
    }

    private async Task<Result<string>> ReplaceFileAsync(
        Domain.Entities.ProviderDocument document,
        UpdateProviderDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var uploadStream = request.FileStream!;
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
                .DetectAsync(uploadStream, request.ContentType!, request.FileName!, cancellationToken)
                .ConfigureAwait(false);

            if (!detection.IsAcceptable)
            {
                return Result.Failure<string>(
                    new Error(
                        "ProviderDocument.UnsupportedType",
                        "File signature, content type, or extension does not match an allowed type (PDF, JPEG, PNG)."),
                    Outcome.Invalid);
            }

            var uploadResult = await fileStorageService
                .UploadAsync(uploadStream, request.FileName!, request.ContentType!, StorageFolder, cancellationToken)
                .ConfigureAwait(false);

            var oldUrl = document.DocumentUrl;
            try
            {
                document.ReplaceFile(uploadResult.Url, request.FileName!);
            }
            catch (BusinessRuleViolationException ex)
            {
                await TryCleanupOrphanedFileAsync(uploadResult.Url, cancellationToken).ConfigureAwait(false);
                return Result.Failure<string>(
                    new Error("ProviderDocument.Invalid", ex.Message),
                    Outcome.Invalid);
            }

            return Result.Success<string>(oldUrl);
        }
        finally
        {
            bufferedStream?.Dispose();
        }
    }

    private async Task TryCleanupOrphanedFileAsync(string fileUrl, CancellationToken ct)
    {
        try
        {
            await fileStorageService.DeleteAsync(fileUrl, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Best-effort cleanup of previous file failed: {FileUrl}",
                fileUrl);
        }
    }
}
