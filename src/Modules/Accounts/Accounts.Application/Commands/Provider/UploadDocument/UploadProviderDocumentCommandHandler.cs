using Accounts.Application.Caching;
using Accounts.Application.Commands.Provider.Shared;
using Accounts.Application.Interfaces;
using Accounts.Domain.Errors;
using ContentCore.Contracts.Storage;
using Accounts.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Storage;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Commands.Provider.UploadDocument;

public sealed class UploadProviderDocumentCommandHandler(
    IProviderApplicationRepository providerApplicationRepository,
    IFileStorageService fileStorageService,
    IFileAssetRegistrar fileAssetRegistrar,
    IProviderDocumentFileWriter providerDocumentFileWriter,
    IAccountsUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<UploadProviderDocumentCommandHandler> logger)
    : ICommandHandler<UploadProviderDocumentCommand, AddProviderDocumentResult>
{
    private const string StorageFolder = "provider-application-documents";

    public async Task<Result<AddProviderDocumentResult>> Handle(
        UploadProviderDocumentCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result<AddProviderDocumentResult>.Failure(
                Error.Unauthorized("Authentication is required."), Outcome.Unauthorized);

        var userId = currentUser.UserId.Value;

        var application = await providerApplicationRepository.GetWithDocumentsByUserIdAsync(userId, cancellationToken);
        if (application is null)
            return Result<AddProviderDocumentResult>.Failure(
                ProviderApplicationErrors.NotFound, Outcome.NotFound);

        // Buffer non-seekable upload streams so the storage provider can read length/seek.
        var uploadStream = request.FileStream;
        MemoryStream? bufferedStream = null;
        try
        {
            if (!uploadStream.CanSeek)
            {
                bufferedStream = new MemoryStream();
                await uploadStream.CopyToAsync(bufferedStream, cancellationToken);
                bufferedStream.Position = 0;
                uploadStream = bufferedStream;
            }

            // Patch 1C — magic-byte/signature validation for sensitive provider documents.
            // The real file content must be an allowed format (PDF/JPEG/PNG) AND agree with
            // the declared content type and extension. Rejects spoofed/mismatched uploads
            // BEFORE anything is written to storage. The inspector rewinds the stream.
            var contentValidation = await ProviderDocumentContentInspector.ValidateAsync(
                uploadStream, request.ContentType, request.FileName, cancellationToken);
            if (contentValidation.IsFailure)
                return Result<AddProviderDocumentResult>.Failure(
                    contentValidation.Errors.Count > 0
                        ? contentValidation.Errors[0]
                        : Error.Validation("file", "Invalid document content."),
                    Outcome.Invalid);

            var uploadResponse = await fileStorageService.UploadAsync(
                uploadStream, request.FileName, request.ContentType, StorageFolder, cancellationToken);

            if (uploadResponse.IsFailure || uploadResponse.Value is null)
                return Result<AddProviderDocumentResult>.Failure(
                    uploadResponse.Errors.Count > 0
                        ? uploadResponse.Errors[0]
                        : new Error("ProviderDocument.UploadFailed", "Failed to store the uploaded document."),
                    Outcome.ServerError);

            var uploaded = uploadResponse.Value;

            // Use the authoritative stored size when available; fall back to client-reported size.
            var fileSizeBytes = uploaded.FileSize > 0 ? uploaded.FileSize : request.FileSizeBytes;

            // Patch 2G: ProviderDocument carries only business metadata now. The physical file
            // (URL / name / size) lives in the FileAsset V2 model.
            var addResult = application.AddDocument(request.DocumentType, request.ExpiresAt);

            if (addResult.IsFailure || addResult.Value is null)
            {
                // Domain rejected the document (duplicate type / too many) — remove the orphaned blob.
                await TryCleanupOrphanedFileAsync(uploaded.Url, cancellationToken);
                return Result<AddProviderDocumentResult>.Failure(
                    addResult.Error ?? Error.Failure("ProviderDocument.Add", "Unknown error occurred while adding document."),
                    Outcome.UnprocessableEntity);
            }

            // Patch 2G: FileAsset registration is now REQUIRED (no longer best-effort). Register the
            // physical file as a FileAsset BEFORE persisting the document so that a registration
            // failure leaves nothing in the database — only the uploaded blob, which we clean up.
            var seed = new FileAssetSeed(
                StorageProvider: "Local",
                StorageKey: uploaded.StorageKey,
                OriginalFileName: request.FileName,
                SafeFileName: SafeFileNameSanitizer.Sanitize(request.FileName, $"document-{addResult.Value.Id:N}"),
                ContentType: request.ContentType,
                Extension: Path.GetExtension(request.FileName ?? string.Empty).ToLowerInvariant(),
                SizeBytes: fileSizeBytes,
                UploadedByUserId: userId);

            var registrarResult = await fileAssetRegistrar.GetOrAddByStorageKeyAsync(seed, dryRun: false, cancellationToken);
            if (registrarResult.IsFailure || registrarResult.Value is null)
            {
                await TryCleanupOrphanedFileAsync(uploaded.Url, cancellationToken);
                logger.LogError(
                    "Patch 2G: FileAsset registration failed for provider document {DocumentId}; upload aborted.",
                    addResult.Value.Id);
                return Result<AddProviderDocumentResult>.Failure(
                    registrarResult.Errors.Count > 0
                        ? registrarResult.Errors[0]
                        : Error.Failure("ProviderDocument.FileAssetFailed", "Failed to register the uploaded document's file metadata."),
                    Outcome.ServerError);
            }

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                await TryCleanupOrphanedFileAsync(uploaded.Url, cancellationToken);
                return Result<AddProviderDocumentResult>.Failure(
                    new Error("ProviderApplication.ConcurrencyConflict", "The provider application was modified concurrently. Reload and retry."),
                    Outcome.Conflict);
            }
            catch
            {
                await TryCleanupOrphanedFileAsync(uploaded.Url, cancellationToken);
                throw;
            }

            // Patch 2G: the ProviderDocumentFile link is now REQUIRED. If it fails, fail the upload,
            // clean up the blob, and compensate by removing the (now link-less, undownloadable)
            // document that was just persisted.
            try
            {
                await providerDocumentFileWriter.UpsertLinkAsync(
                    addResult.Value.Id, registrarResult.Value.Id, request.DocumentType, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await TryCleanupOrphanedFileAsync(uploaded.Url, cancellationToken);

                try
                {
                    application.RemoveDocument(addResult.Value.Id);
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                }
                catch (Exception compensationEx) when (compensationEx is not OperationCanceledException)
                {
                    logger.LogError(compensationEx,
                        "Patch 2G: compensation removal of unlinked provider document {DocumentId} failed; manual cleanup may be required.",
                        addResult.Value.Id);
                }

                logger.LogError(ex,
                    "Patch 2G: FileAsset link creation failed for provider document {DocumentId}; upload aborted.",
                    addResult.Value.Id);
                return Result<AddProviderDocumentResult>.Failure(
                    Error.Failure("ProviderDocument.LinkFailed", "Failed to link the uploaded document to its stored file."),
                    Outcome.ServerError);
            }

            await cache.RemoveByTagAsync(AccountsCacheKeys.MyApplicationStatusTag(userId), cancellationToken);

            logger.LogInformation(
                "Document {DocumentType} uploaded to application {ApplicationId} for user {UserId} ({Bytes} bytes)",
                request.DocumentType, application.Id, userId, fileSizeBytes);

            return Result<AddProviderDocumentResult>.Created(
                new AddProviderDocumentResult(addResult.Value.Id, addResult.Value.DocumentType));
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
            await fileStorageService.DeleteAsync(fileUrl, ct);
            logger.LogWarning("Document persistence failed after upload — cleaned up orphaned file: {FileUrl}", fileUrl);
        }
        catch (Exception cleanupEx)
        {
            logger.LogError(cleanupEx,
                "Document persistence failed AND cleanup failed — orphaned file may need manual removal: {FileUrl}", fileUrl);
        }
    }
}
