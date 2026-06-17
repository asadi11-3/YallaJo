using Accounts.Application.Caching;
using Accounts.Application.Commands.Provider.ReplaceDocument;
using Accounts.Application.Commands.Provider.Shared;
using Accounts.Application.Interfaces;
using Accounts.Domain.Errors;
using Accounts.Domain.Repositories;
using ContentCore.Contracts.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Storage;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Commands.Provider.ReplaceDocumentUpload;

/// <summary>
/// Stores the uploaded replacement file, then swaps the document metadata —
/// mirrors <see cref="UploadDocument.UploadProviderDocumentCommandHandler"/> storage handling.
/// </summary>
public sealed class ReplaceProviderDocumentUploadCommandHandler(
    IProviderApplicationRepository providerApplicationRepository,
    IFileStorageService fileStorageService,
    IFileAssetRegistrar fileAssetRegistrar,
    IProviderDocumentFileWriter providerDocumentFileWriter,
    IAccountsUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<ReplaceProviderDocumentUploadCommandHandler> logger)
    : ICommandHandler<ReplaceProviderDocumentUploadCommand, ReplaceProviderDocumentResult>
{
    private const string StorageFolder = "provider-application-documents";

    public async Task<Result<ReplaceProviderDocumentResult>> Handle(
        ReplaceProviderDocumentUploadCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result<ReplaceProviderDocumentResult>.Failure(
                Error.Unauthorized("Authentication is required."), Outcome.Unauthorized);

        var userId = currentUser.UserId.Value;

        var application = await providerApplicationRepository.GetWithDocumentsByUserIdAsync(userId, cancellationToken);
        if (application is null)
            return Result<ReplaceProviderDocumentResult>.Failure(
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
            // Rejects spoofed/mismatched replacement uploads BEFORE writing to storage.
            // The inspector rewinds the stream so storage reads from the start.
            var contentValidation = await ProviderDocumentContentInspector.ValidateAsync(
                uploadStream, request.ContentType, request.FileName, cancellationToken);
            if (contentValidation.IsFailure)
                return Result<ReplaceProviderDocumentResult>.Failure(
                    contentValidation.Errors.Count > 0
                        ? contentValidation.Errors[0]
                        : Error.Validation("file", "Invalid document content."),
                    Outcome.Invalid);

            var uploadResponse = await fileStorageService.UploadAsync(
                uploadStream, request.FileName, request.ContentType, StorageFolder, cancellationToken);

            if (uploadResponse.IsFailure || uploadResponse.Value is null)
                return Result<ReplaceProviderDocumentResult>.Failure(
                    uploadResponse.Errors.Count > 0
                        ? uploadResponse.Errors[0]
                        : new Error("ProviderDocument.UploadFailed", "Failed to store the uploaded document."),
                    Outcome.ServerError);

            var uploaded = uploadResponse.Value;
            var fileSizeBytes = uploaded.FileSize > 0 ? uploaded.FileSize : request.FileSizeBytes;

            // The replaced document keeps its original DocumentType — resolve it from the
            // already-loaded aggregate before any mutation (replace does not change the type).
            var existingDocument = application.Documents.FirstOrDefault(d => d.Id == request.DocumentId);
            if (existingDocument is null)
            {
                await TryCleanupOrphanedFileAsync(uploaded.Url, cancellationToken);
                return Result<ReplaceProviderDocumentResult>.Failure(
                    ProviderApplicationErrors.DocumentNotFound, Outcome.NotFound);
            }

            var documentType = existingDocument.DocumentType;

            // Patch 2G: FileAsset registration + ProviderDocumentFile link are REQUIRED (no longer
            // best-effort). Register the FileAsset for the NEW blob FIRST. If it fails, clean up the
            // new blob and abort — the prior document/link/FileAsset remain intact and consistent.
            var seed = new FileAssetSeed(
                StorageProvider: "Local",
                StorageKey: uploaded.StorageKey,
                OriginalFileName: request.FileName,
                SafeFileName: SafeFileNameSanitizer.Sanitize(request.FileName, $"document-{request.DocumentId:N}"),
                ContentType: request.ContentType,
                Extension: Path.GetExtension(request.FileName ?? string.Empty).ToLowerInvariant(),
                SizeBytes: fileSizeBytes,
                UploadedByUserId: userId);

            var registrarResult = await fileAssetRegistrar.GetOrAddByStorageKeyAsync(seed, dryRun: false, cancellationToken);
            if (registrarResult.IsFailure || registrarResult.Value is null)
            {
                await TryCleanupOrphanedFileAsync(uploaded.Url, cancellationToken);
                logger.LogError(
                    "Patch 2G: FileAsset registration failed for replaced provider document {DocumentId}; replacement aborted, prior file unchanged.",
                    request.DocumentId);
                return Result<ReplaceProviderDocumentResult>.Failure(
                    registrarResult.Errors.Count > 0
                        ? registrarResult.Errors[0]
                        : Error.Failure("ProviderDocument.FileAssetFailed", "Failed to register the replacement file metadata."),
                    Outcome.ServerError);
            }

            var replaceResult = application.ReplaceDocument(request.DocumentId, request.ExpiresAt);

            if (replaceResult.IsFailure)
            {
                await TryCleanupOrphanedFileAsync(uploaded.Url, cancellationToken);
                return Result<ReplaceProviderDocumentResult>.Failure(
                    replaceResult.Errors.FirstOrDefault()!, Outcome.UnprocessableEntity);
            }

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                await TryCleanupOrphanedFileAsync(uploaded.Url, cancellationToken);
                return Result<ReplaceProviderDocumentResult>.Failure(
                    new Error("ProviderApplication.ConcurrencyConflict", "The provider application was modified concurrently. Reload and retry."),
                    Outcome.Conflict);
            }
            catch
            {
                await TryCleanupOrphanedFileAsync(uploaded.Url, cancellationToken);
                throw;
            }

            // Repoint the ProviderDocumentFile link to the newly registered FileAsset. If this fails,
            // clean up the new blob and fail — the link still points at the prior FileAsset, so the
            // document remains downloadable and consistent (no document compensation required).
            try
            {
                await providerDocumentFileWriter.UpsertLinkAsync(
                    request.DocumentId, registrarResult.Value.Id, documentType, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await TryCleanupOrphanedFileAsync(uploaded.Url, cancellationToken);
                logger.LogError(ex,
                    "Patch 2G: FileAsset link repoint failed for replaced provider document {DocumentId}; replacement aborted, prior link retained.",
                    request.DocumentId);
                return Result<ReplaceProviderDocumentResult>.Failure(
                    Error.Failure("ProviderDocument.LinkFailed", "Failed to link the replacement document to its stored file."),
                    Outcome.ServerError);
            }

            await cache.RemoveByTagAsync(AccountsCacheKeys.MyApplicationStatusTag(userId), cancellationToken);

            logger.LogInformation(
                "Document {DocumentId} replaced via upload in application {ApplicationId} for user {UserId} ({Bytes} bytes)",
                request.DocumentId, application.Id, userId, fileSizeBytes);

            return Result<ReplaceProviderDocumentResult>.Success(
                new ReplaceProviderDocumentResult(request.DocumentId));
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
            logger.LogWarning("Replacement persistence failed after upload — cleaned up orphaned file: {FileUrl}", fileUrl);
        }
        catch (Exception cleanupEx)
        {
            logger.LogError(cleanupEx,
                "Replacement persistence failed AND cleanup failed — orphaned file may need manual removal: {FileUrl}", fileUrl);
        }
    }
}
