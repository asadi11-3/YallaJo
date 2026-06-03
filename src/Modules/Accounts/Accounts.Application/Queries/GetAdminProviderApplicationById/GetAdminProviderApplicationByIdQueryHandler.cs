using Accounts.Domain.Errors;
using Accounts.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Queries.GetAdminProviderApplicationById;

public sealed class GetAdminProviderApplicationByIdQueryHandler(
    IProviderApplicationRepository providerApplicationRepository,
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

        var documents = application.Documents
            .Select(d => new AdminProviderDocumentDto(
                DocumentId: d.Id,
                DocumentType: d.DocumentType,
                FileUrl: d.FileUrl,
                FileName: d.FileName,
                FileSizeBytes: d.FileSizeBytes,
                UploadedAt: d.UploadedAt,
                ExpiresAt: d.ExpiresAt))
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
