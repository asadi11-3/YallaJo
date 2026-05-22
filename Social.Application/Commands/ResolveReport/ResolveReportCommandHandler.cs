using MediatR;
using Microsoft.Extensions.Logging;
using Social.Application.Interfaces;
using Social.Domain.Entities;
using Social.Domain.Enums;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Commands.ResolveReport;

internal sealed class ResolveReportCommandHandler(
    IReportRepository reportRepository,
    IReviewRepository reviewRepository,
    IContentModerationLogRepository moderationLogRepository,
    ISocialUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<ResolveReportCommandHandler> logger)
    : IRequestHandler<ResolveReportCommand, Result>
{
    public async Task<Result> Handle(ResolveReportCommand request, CancellationToken ct)
    {
        var report = await reportRepository.GetByIdAsync(request.ReportId, ct);
        if (report is null)
        {
            return Result.Failure(
                new Error("Report.NotFound", $"Report {request.ReportId} not found."),
                Outcome.NotFound);
        }

        if (report.Status is ReportStatus.Resolved or ReportStatus.Dismissed)
        {
            return Result.Failure(
                new Error("Report.AlreadyResolved", "This report has already been resolved."),
                Outcome.Conflict);
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Resolve the report aggregate
        report.Resolve(request.AdminUserId, request.Action, request.Notes, timeProvider);

        // Act on the underlying entity when it's a Review
        if (report.EntityType == ReportableEntityType.Review)
        {
            var review = await reviewRepository.GetByIdAsync(report.EntityId, ct);
            if (review is not null)
            {
                switch (request.Action)
                {
                    case ModerationAction.RemoveContent:
                        review.Delete(ReviewDeletionSource.Admin, timeProvider);
                        break;
                    case ModerationAction.RestoreContent:
                        review.Restore(request.AdminUserId, timeProvider);
                        break;
                    // Dismiss / WarnUser / BanUser affect only the report, not the review content
                }
            }
        }

        // Append immutable moderation log
        var log = ContentModerationLog.Create(
            request.AdminUserId,
            report.EntityType,
            report.EntityId,
            request.Action,
            request.Notes,
            now,
            sourceReportId: report.Id);

        await moderationLogRepository.AddAsync(log, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Admin {AdminId} resolved report {ReportId} with action {Action}",
            request.AdminUserId, request.ReportId, request.Action);

        return Result.Success();
    }
}
