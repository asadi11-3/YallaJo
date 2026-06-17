using Accounts.Application.Commands.Provider.Shared;
using Accounts.Domain.Entities;
using Accounts.Domain.Repositories;
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
/// Authorization rules:
/// <list type="bullet">
/// <item>Anonymous / unauthenticated -> Unauthorized.</item>
/// <item>The owner of the document's parent application -> allowed.</item>
/// <item>An admin-tier caller (role privilege >= Admin) -> allowed for any application.</item>
/// <item>Any other authenticated caller -> NotFound (we deliberately do NOT distinguish
/// "exists but not yours" from "does not exist" to prevent document-id enumeration).</item>
/// </list>
/// </para>
/// </summary>
public sealed class DownloadProviderDocumentQueryHandler(
    IProviderApplicationRepository providerApplicationRepository,
    IFileStorageService fileStorageService,
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

        // 4) Open the underlying file. Storage guards path traversal and returns
        //    a failure result (NotFound/Invalid) rather than throwing.
        var openResult = await fileStorageService.OpenReadAsync(document.FileUrl, cancellationToken);
        if (openResult.IsFailure)
        {
            logger.LogWarning(
                "Provider document blob unavailable. DocumentId={DocumentId}, Outcome={Outcome}",
                request.DocumentId, openResult.Outcome);
            return Result<DownloadProviderDocumentResult>.Failure(
                Error.NotFound("ProviderDocument"), Outcome.NotFound);
        }

        var download = openResult.Value!;
        var safeFileName = BuildSafeDownloadName(document);

        logger.LogInformation(
            "Provider document streamed. DocumentId={DocumentId}, RequestedBy={UserId}, AdminTier={IsAdminTier}",
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
