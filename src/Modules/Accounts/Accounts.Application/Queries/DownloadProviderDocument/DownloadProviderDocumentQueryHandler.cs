using Accounts.Application.Commands.Provider.Shared;
using Accounts.Domain.Entities;
using Accounts.Domain.Repositories;
using ContentCore.Contracts.Storage;
using Microsoft.Extensions.Logging;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Storage;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Queries.DownloadProviderDocument;

/// <summary>
/// Authorizes and streams a provider application document.
/// <para>
/// Authorization rules (Patch 1A, preserved byte-for-byte by Patch 2C):
/// <list type="bullet">
/// <item>Anonymous / unauthenticated -> Unauthorized.</item>
/// <item>The owner of the document's parent application -> allowed.</item>
/// <item>An admin-tier caller (role privilege >= Admin) -> allowed for any application.</item>
/// <item>Any other authenticated caller -> NotFound (we deliberately do NOT distinguish
/// "exists but not yours" from "does not exist" to prevent document-id enumeration).</item>
/// </list>
/// </para>
/// <para>
/// Read-source preference (Patch 2C expand-and-contract):
/// <list type="number">
/// <item>If a ProviderDocumentFile link row exists for this document and the linked
/// FileAsset is found and the blob is readable by its storage key, stream via the
/// FileAsset V2 path (the new authoritative source).</item>
/// <item>Otherwise fall back to the legacy ProviderDocument.FileUrl path, kept until
/// the Patch 2B backfill has been verified complete and FileUrl is dropped (Patch 2G).</item>
/// </list>
/// In neither case does StorageKey / FileUrl / physical path leak to the API client
/// or to information-level logs.
/// </para>
/// </summary>
public sealed class DownloadProviderDocumentQueryHandler(
    IProviderApplicationRepository providerApplicationRepository,
    IFileStorageService fileStorageService,
    IFileAssetLocator fileAssetLocator,
    ICurrentUser currentUser,
    ILogger<DownloadProviderDocumentQueryHandler> logger)
    : IQueryHandler<DownloadProviderDocumentQuery, DownloadProviderDocumentResult>
{
    public async Task<Result<DownloadProviderDocumentResult>> Handle(
        DownloadProviderDocumentQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result<DownloadProviderDocumentResult>.Failure(
                Error.Unauthorized("Authentication is required."), Outcome.Unauthorized);

        var userId = currentUser.UserId.Value;

        // 1) Owner path: the document must belong to the caller's own application.
        var application = await providerApplicationRepository
            .GetWithDocumentsByUserIdAsync(userId, cancellationToken);

        var document = application?.Documents.FirstOrDefault(d => d.Id == request.DocumentId);

        // 2) Admin-tier path: allow access to any application's documents.
        var isAdminTier = AppRoles.HighestPrivilegeLevel(currentUser.Roles) >= RolePrivilegeLevel.Admin;
        if (document is null && isAdminTier)
        {
            var ownerApplication = await providerApplicationRepository
                .GetWithDocumentsByDocumentIdAsync(request.DocumentId, cancellationToken);
            document = ownerApplication?.Documents.FirstOrDefault(d => d.Id == request.DocumentId);
        }

        // 3) Not owned and not admin -> NotFound (no existence leak / enumeration).
        if (document is null)
        {
            logger.LogInformation(
                "Provider document download denied or missing. DocumentId={DocumentId}, RequestedBy={UserId}, AdminTier={IsAdminTier}",
                request.DocumentId, userId, isAdminTier);
            return Result<DownloadProviderDocumentResult>.Failure(
                Error.NotFound("ProviderDocument"), Outcome.NotFound);
        }

        // 4) Preferred path: resolve via ProviderDocumentFile -> FileAsset -> StorageKey.
        //    The link is an opaque cross-module reference (no DB FK by design).
        var fileAssetId = await providerApplicationRepository
            .GetFileAssetIdByDocumentIdAsync(document.Id, cancellationToken);

        if (fileAssetId.HasValue)
        {
            var viewResult = await fileAssetLocator.GetByIdAsync(fileAssetId.Value, cancellationToken);
            if (viewResult.IsSuccess && viewResult.Value is not null)
            {
                var view = viewResult.Value;
                var openByKeyResult = await fileStorageService.OpenReadByStorageKeyAsync(
                    view.StorageKey, cancellationToken);

                if (openByKeyResult.IsSuccess && openByKeyResult.Value is not null)
                {
                    var fileAssetDownload = openByKeyResult.Value;

                    logger.LogInformation(
                        "Provider document streamed via FileAsset path. DocumentId={DocumentId}, FileAssetId={FileAssetId}, RequestedBy={UserId}, AdminTier={IsAdminTier}",
                        request.DocumentId, fileAssetId.Value, userId, isAdminTier);

                    return Result<DownloadProviderDocumentResult>.Success(new DownloadProviderDocumentResult(
                        Content: fileAssetDownload.Content,
                        ContentType: view.ContentType,
                        FileName: SafeFileNameSanitizer.Sanitize(
                            view.OriginalFileName, $"document-{document.Id:N}"),
                        FileSize: view.SizeBytes));
                }
            }
        }

        // 5) Patch 2C expand-and-contract: legacy FileUrl path retained as fallback
        //    for ProviderDocument rows that the Patch 2B backfill has not yet linked
        //    into FileAssets. This branch will be removed in Patch 2G after backfill
        //    is verified complete and FileUrl is dropped.
        var openResult = await fileStorageService.OpenReadAsync(document.FileUrl, cancellationToken);
        if (openResult.IsFailure || openResult.Value is null)
        {
            logger.LogWarning(
                "Provider document blob unavailable on both FileAsset and legacy paths. DocumentId={DocumentId}, Outcome={Outcome}",
                request.DocumentId, openResult.Outcome);
            return Result<DownloadProviderDocumentResult>.Failure(
                Error.NotFound("ProviderDocument"), Outcome.NotFound);
        }

        var download = openResult.Value;
        var safeFileName = BuildSafeDownloadName(document);

        logger.LogInformation(
            "Provider document streamed via legacy FileUrl path. DocumentId={DocumentId}, RequestedBy={UserId}, AdminTier={IsAdminTier}",
            request.DocumentId, userId, isAdminTier);

        return Result<DownloadProviderDocumentResult>.Success(new DownloadProviderDocumentResult(
            Content: download.Content,
            ContentType: download.ContentType,
            FileName: safeFileName,
            FileSize: download.FileSize));
    }

    /// <summary>
    /// Produces a safe download file name from the stored original name, stripping any
    /// directory components and invalid characters. Never exposes the storage key/path.
    /// </summary>
    private static string BuildSafeDownloadName(ProviderDocument document)
        => SafeFileNameSanitizer.Sanitize(document.FileName, $"document-{document.Id:N}");
}
