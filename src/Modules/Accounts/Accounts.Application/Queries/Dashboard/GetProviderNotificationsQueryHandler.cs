using Accounts.Application.Caching;
using Accounts.Domain.Enums;
using Accounts.Domain.Errors;
using Accounts.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Queries.Dashboard;

public sealed class GetProviderNotificationsQueryHandler(
    IProviderApplicationRepository providerApplicationRepository,
    ICurrentUser currentUser,
    HybridCache cache)
    : IQueryHandler<GetProviderNotificationsQuery, IReadOnlyList<ProviderNotificationDto>>
{
    public async Task<Result<IReadOnlyList<ProviderNotificationDto>>> Handle(
        GetProviderNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;

        var result = await cache.GetOrCreateAsync(
            $"accounts:provider:notifications:{userId}",
            async ct =>
            {
                var application = await providerApplicationRepository.GetWithDocumentsByUserIdAsync(userId, ct);
                if (application is null)
                    return (IReadOnlyList<ProviderNotificationDto>?)null;

                var notifications = new List<ProviderNotificationDto>();
                var now = DateTime.UtcNow;

                // Application status notifications
                if (application.Status == ProviderApplicationStatus.Approved && application.ReviewedAt.HasValue)
                {
                    notifications.Add(new ProviderNotificationDto(
                        "APPLICATION_APPROVED",
                        "Application Approved",
                        "Your provider application has been approved. You can now manage your content.",
                        application.Id.ToString(),
                        application.ReviewedAt.Value,
                        IsRead: true));
                }

                if (application.Status == ProviderApplicationStatus.Rejected && application.ReviewedAt.HasValue)
                {
                    notifications.Add(new ProviderNotificationDto(
                        "APPLICATION_REJECTED",
                        "Application Rejected",
                        $"Your provider application was rejected. Reason: {application.RejectionReason ?? "Not specified"}",
                        application.Id.ToString(),
                        application.ReviewedAt.Value,
                        IsRead: false));
                }

                if (application.Status == ProviderApplicationStatus.MoreDocsNeeded && application.ReviewedAt.HasValue)
                {
                    notifications.Add(new ProviderNotificationDto(
                        "MORE_DOCS_REQUESTED",
                        "Additional Documents Required",
                        "An admin has requested additional documents for your application.",
                        application.Id.ToString(),
                        application.ReviewedAt.Value,
                        IsRead: false));
                }

                // Document expiry notifications
                foreach (var doc in application.Documents.Where(d => d.ExpiresAt.HasValue))
                {
                    if (doc.ExpiresAt!.Value < now)
                    {
                        notifications.Add(new ProviderNotificationDto(
                            "DOCUMENT_EXPIRED",
                            $"Document Expired: {doc.DocumentType}",
                            $"Your {doc.DocumentType} document has expired. Please upload a renewed copy.",
                            doc.Id.ToString(),
                            doc.ExpiresAt.Value,
                            IsRead: false));
                    }
                    else if (doc.ExpiresAt.Value <= now.AddDays(30))
                    {
                        notifications.Add(new ProviderNotificationDto(
                            "DOCUMENT_EXPIRING_SOON",
                            $"Document Expiring: {doc.DocumentType}",
                            $"Your {doc.DocumentType} document expires on {doc.ExpiresAt.Value:yyyy-MM-dd}. Please renew.",
                            doc.Id.ToString(),
                            now,
                            IsRead: false));
                    }
                }

                // Reapplication available
                if (application.Status == ProviderApplicationStatus.Rejected &&
                    application.CoolingPeriodEndsAt.HasValue &&
                    application.CoolingPeriodEndsAt.Value <= now &&
                    application.ReapplicationCount < 3)
                {
                    notifications.Add(new ProviderNotificationDto(
                        "REAPPLICATION_AVAILABLE",
                        "Reapplication Available",
                        "Your cooling period has ended. You may reapply for provider status.",
                        application.Id.ToString(),
                        application.CoolingPeriodEndsAt.Value,
                        IsRead: false));
                }

                return (IReadOnlyList<ProviderNotificationDto>)notifications
                    .OrderByDescending(n => n.OccurredAt)
                    .ToList()
                    .AsReadOnly();
            },
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(2) },
            tags: [AccountsCacheKeys.MyApplicationStatusTag(userId)],
            cancellationToken: cancellationToken);

        if (result is null)
            return Result<IReadOnlyList<ProviderNotificationDto>>.Failure(
                ProviderApplicationErrors.NotFound, Outcome.NotFound);

        return Result<IReadOnlyList<ProviderNotificationDto>>.Success(result);
    }
}
