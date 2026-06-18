using Booking.Contracts.Authorization;
using Booking.Domain.Entities;
using Booking.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Storage;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Application.Queries.DownloadProviderDocument;

/// <summary>
/// Resolves and streams a Booking provider document for an authorized caller.
/// <para>
/// Authorization mirrors <c>GetProviderDocumentByIdQueryHandler</c>: anonymous callers get
/// <see cref="Outcome.Unauthorized"/>; admins (AdminBookingDashboard.Read) may download any
/// document; owners may download their own; everything else (cross-provider or missing)
/// returns <see cref="Outcome.NotFound"/> so document ids cannot be enumerated.
/// </para>
/// <para>
/// The file bytes are resolved internally via <see cref="IFileStorageService.OpenReadAsync"/>.
/// The stored URL, storage key, physical path and raw filename are NEVER logged or returned —
/// only a sanitized download filename is surfaced.
/// </para>
/// </summary>
public sealed class DownloadProviderDocumentQueryHandler(
    IProviderDocumentRepository documentRepository,
    IFileStorageService fileStorageService,
    ICurrentUser currentUser,
    ILogger<DownloadProviderDocumentQueryHandler> logger)
    : IQueryHandler<DownloadProviderDocumentQuery, DownloadProviderDocumentResult>
{
    public async Task<Result<DownloadProviderDocumentResult>> Handle(
        DownloadProviderDocumentQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            {
                return Result.Failure<DownloadProviderDocumentResult>(
                    new Error("ProviderDocument.Unauthorized", "Authentication is required."),
                    Outcome.Unauthorized);
            }

            var isAdmin = currentUser.HasPermission(
                $"{BookingFeatures.AdminBookingDashboard}.{AppAction.Read}");

            ProviderDocument? document;

            if (isAdmin)
            {
                document = await documentRepository
                    .GetByIdAsync(request.Id, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                var tourGuideId = await documentRepository
                    .GetTourGuideIdByUserIdAsync(currentUser.UserId.Value, cancellationToken)
                    .ConfigureAwait(false);

                document = tourGuideId is null
                    ? null
                    : await documentRepository
                        .GetByIdForTourGuideAsync(request.Id, tourGuideId.Value, cancellationToken)
                        .ConfigureAwait(false);
            }

            if (document is null)
            {
                return Result.Failure<DownloadProviderDocumentResult>(
                    new Error("ProviderDocument.NotFound", "Provider document was not found."),
                    Outcome.NotFound);
            }

            var openResult = await fileStorageService
                .OpenReadAsync(document.DocumentUrl, cancellationToken)
                .ConfigureAwait(false);

            if (!openResult.IsSuccess || openResult.Value is null)
            {
                // Storage miss (orphaned link / deleted blob). Surface NotFound without
                // leaking the underlying URL or storage details.
                logger.LogWarning(
                    "Provider document {DocumentId} has no readable file backing; returning NotFound.",
                    document.Id);

                return Result.Failure<DownloadProviderDocumentResult>(
                    new Error("ProviderDocument.NotFound", "Provider document was not found."),
                    Outcome.NotFound);
            }

            var download = openResult.Value;
            var fileName = BuildDownloadFileName(document.OriginalFileName, document.Id);

            logger.LogDebug(
                "Provider document {DocumentId} streamed for user {UserId} (admin={IsAdmin}).",
                document.Id,
                currentUser.UserId,
                isAdmin);

            return Result.Success<DownloadProviderDocumentResult>(new DownloadProviderDocumentResult(
                download.Content,
                download.ContentType,
                fileName,
                download.FileSize));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<DownloadProviderDocumentResult>(
                new Error("Request.Cancelled", "The request was cancelled by the client."),
                Outcome.Canceled);
        }
    }

    /// <summary>
    /// Produces a safe Content-Disposition filename from the stored display name, stripping any
    /// directory components so a crafted <c>OriginalFileName</c> cannot leak or traverse paths.
    /// Falls back to a deterministic, non-sensitive name when no usable display name exists.
    /// </summary>
    private static string BuildDownloadFileName(string? originalFileName, Guid documentId)
    {
        var fallback = $"document-{documentId:N}";

        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            return fallback;
        }

        // Defensively strip any path information regardless of separator style.
        var trimmed = originalFileName.Replace('\\', '/');
        var leaf = trimmed[(trimmed.LastIndexOf('/') + 1)..].Trim();

        return string.IsNullOrWhiteSpace(leaf) ? fallback : leaf;
    }
}
