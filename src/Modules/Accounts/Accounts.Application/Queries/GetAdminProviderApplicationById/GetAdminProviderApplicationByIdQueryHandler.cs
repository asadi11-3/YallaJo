using Accounts.Domain.Errors;
using Accounts.Domain.Repositories;
using ContentCore.Contracts.Storage;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Queries.GetAdminProviderApplicationById;

public sealed class GetAdminProviderApplicationByIdQueryHandler(
    IProviderApplicationRepository providerApplicationRepository,
    IFileAssetLocator fileAssetLocator,
    ILogger<GetAdminProviderApplicationByIdQueryHandler> logger)
    : IQueryHandler<GetAdminProviderApplicationByIdQuery, AdminProviderApplicationDetailsResult>
{
    public async Task<Result<AdminProviderApplicationDetailsResult>> Handle(
        GetAdminProviderApplicationByIdQuery request,
        CancellationToken cancellationToken)
    {
        var application = await providerApplicationRepository.GetWithDocumentsAsync(
            request.ApplicationId, cancellationToken);

        if (application is null)
            return Result<AdminProviderApplicationDetailsResult>.Failure(
                ProviderApplicationErrors.NotFound, Outcome.NotFound);

        // Patch 2G: FileAsset V2 is now the only source of document display metadata.
        // Two batched round-trips (doc->fileAssetId map, then fileAssetId->view map) keep
        // this N+1-free regardless of document count. StorageKey / physical path / internal
        // FileAsset id are never projected into the DTO.
        var documentIds = application.Documents.Select(d => d.Id).ToList();
        var docToFileAssetId = await providerApplicationRepository
            .GetFileAssetIdsByDocumentIdsAsync(documentIds, cancellationToken);
        var fileAssetViews = await fileAssetLocator
            .GetByIdsAsync(docToFileAssetId.Values.ToList(), cancellationToken);

        var documents = application.Documents
            .Select(d =>
            {
                var fileName = string.Empty;
                long fileSizeBytes = 0;
                if (docToFileAssetId.TryGetValue(d.Id, out var fileAssetId)
                    && fileAssetViews.TryGetValue(fileAssetId, out var view))
                {
                    fileName = view.OriginalFileName;
                    fileSizeBytes = view.SizeBytes;
                }

                return new AdminProviderDocumentDto(
                    DocumentId: d.Id,
                    DocumentType: d.DocumentType,
                    FileName: fileName,
                    FileSizeBytes: fileSizeBytes,
                    UploadedAt: d.UploadedAt,
                    ExpiresAt: d.ExpiresAt);
            })
            .ToList();

        logger.LogDebug("Admin fetched provider application {ApplicationId} (status {Status}, {DocCount} docs)",
            application.Id, application.Status, documents.Count);

        return Result<AdminProviderApplicationDetailsResult>.Success(new AdminProviderApplicationDetailsResult(
            ApplicationId: application.Id,
            UserId: application.UserId,
            ContactEmail: application.ContactEmail,
            Type: application.Type,
            BusinessName: application.BusinessName,
            ContactPhone: application.ContactPhone,
            Address: application.Address,
            Description: application.Description,
            Status: application.Status,
            SubmittedAt: application.SubmittedAt,
            ReviewedAt: application.ReviewedAt,
            ReviewedByUserId: application.ReviewedByUserId,
            RejectionReason: application.RejectionReason,
            SuspensionReason: application.SuspensionReason,
            ReapplicationCount: application.ReapplicationCount,
            CoolingPeriodEndsAt: application.CoolingPeriodEndsAt,
            MissingDocumentTypes: application.GetMissingDocumentTypes(),
            Documents: documents));
    }
}
