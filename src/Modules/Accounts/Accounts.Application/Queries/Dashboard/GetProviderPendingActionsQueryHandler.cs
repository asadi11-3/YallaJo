using Accounts.Domain.Entities;
using Accounts.Domain.Enums;
using Accounts.Domain.Errors;
using Accounts.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Queries.Dashboard;

public sealed class GetProviderPendingActionsQueryHandler(
    IProviderApplicationRepository providerApplicationRepository,
    ICurrentUser currentUser)
    : IQueryHandler<GetProviderPendingActionsQuery, IReadOnlyList<ProviderPendingAction>>
{
    public async Task<Result<IReadOnlyList<ProviderPendingAction>>> Handle(
        GetProviderPendingActionsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;

        var application = await providerApplicationRepository.GetWithDocumentsByUserIdAsync(userId, cancellationToken);
        if (application is null)
            return Result<IReadOnlyList<ProviderPendingAction>>.Failure(
                ProviderApplicationErrors.NotFound, Outcome.NotFound);

        var actions = new List<ProviderPendingAction>();
        var now = DateTime.UtcNow;

        // Check for expired documents
        foreach (var doc in application.Documents.Where(d => d.ExpiresAt.HasValue && d.ExpiresAt.Value < now))
        {
            actions.Add(new ProviderPendingAction(
                "DOCUMENT_EXPIRED",
                $"Document '{doc.DocumentType}' has expired. Please upload a renewed copy.",
                doc.Id.ToString(),
                doc.ExpiresAt));
        }

        // Check for documents expiring in 30 days
        foreach (var doc in application.Documents.Where(d =>
            d.ExpiresAt.HasValue && d.ExpiresAt.Value >= now && d.ExpiresAt.Value <= now.AddDays(30)))
        {
            actions.Add(new ProviderPendingAction(
                "DOCUMENT_EXPIRING_SOON",
                $"Document '{doc.DocumentType}' expires soon. Please renew before it expires.",
                doc.Id.ToString(),
                doc.ExpiresAt));
        }

        // Check for more docs requested
        if (application.Status == ProviderApplicationStatus.MoreDocsNeeded)
        {
            actions.Add(new ProviderPendingAction(
                "MORE_DOCS_REQUIRED",
                "Admin has requested additional documents. Please upload and resubmit your application.",
                application.Id.ToString(),
                null));
        }

        // Check for rejected application within reapplication window
        if (application.Status == ProviderApplicationStatus.Rejected &&
            application.CoolingPeriodEndsAt.HasValue &&
            application.CoolingPeriodEndsAt.Value <= now &&
            application.ReapplicationCount < 3)
        {
            actions.Add(new ProviderPendingAction(
                "REAPPLICATION_AVAILABLE",
                "Your application was rejected but you are eligible to reapply.",
                application.Id.ToString(),
                null));
        }

        return Result<IReadOnlyList<ProviderPendingAction>>.Success(actions.AsReadOnly());
    }
}
