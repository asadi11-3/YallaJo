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
/// Read source (Patch 2G — FileAsset V2 is now the only authoritative source):
/// the document is streamed via ProviderDocument -> ProviderDocumentFile -> FileAsset
/// -> StorageKey. If no link row / FileAsset / readable blob exists, the handler returns
/// NotFound. The legacy ProviderDocument.FileUrl fallback (Patch 2C) was removed here.
/// StorageKey / physical path never leak to the API client or to information-level logs.
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

        // 5) Patch 2G: FileAsset V2 is the only authoritative source. If no link row /
        //    FileAsset / readable blob was resolved above, the document is not downloadable.
        logger.LogInformation(
            "Provider document has no resolvable FileAsset. DocumentId={DocumentId}, RequestedBy={UserId}, AdminTier={IsAdminTier}",
            request.DocumentId, userId, isAdminTier);

        return Result<DownloadProviderDocumentResult>.Failure(
            Error.NotFound("ProviderDocument"), Outcome.NotFound);
    }
}
