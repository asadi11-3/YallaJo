using MediatR;
using Microsoft.Extensions.Logging;
using Social.Application.Interfaces;
using Social.Domain.Entities;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Social.Application.Commands.SubmitReport;

internal sealed class SubmitReportCommandHandler(
    IReportRepository reportRepository,
    IReviewRepository reviewRepository,
    ISocialUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<SubmitReportCommandHandler> logger)
    : IRequestHandler<SubmitReportCommand, Result<Guid>>
{
    private const int AutoHideThreshold = 5;

    public async Task<Result<Guid>> Handle(SubmitReportCommand request, CancellationToken ct)
    {
        // Idempotency: reject duplicate open report from the same user for the same entity
        var alreadyReported = await reportRepository.ExistsOpenByReporterAsync(
            request.ReporterUserId, request.EntityType, request.EntityId, ct);

        if (alreadyReported)
        {
            return Result.Failure<Guid>(
                new Error("Report.AlreadyReported", "You have already submitted an open report for this item."),
                Outcome.Conflict);
        }

        var report = Report.Submit(
            request.ReporterUserId,
            request.EntityType,
            request.EntityId,
            request.Reason,
            request.Description,
            timeProvider);

        await reportRepository.AddAsync(report, ct);

        // S-R5: if 5 unique reporters → auto-hide the review
        if (request.EntityType == Social.Domain.Enums.ReportableEntityType.Review)
        {
            var uniqueReporterCount = await reportRepository.CountUniqueReportersAsync(
                request.EntityType, request.EntityId, ct);

            // +1 for the just-added report (not yet committed but count is DB-based; add 1)
            var effectiveCount = uniqueReporterCount + 1;

            if (effectiveCount >= AutoHideThreshold)
            {
                var review = await reviewRepository.GetByIdAsync(request.EntityId, ct);
                if (review is not null)
                {
                    review.AutoHide(effectiveCount, timeProvider);
                    logger.LogInformation(
                        "Review {ReviewId} auto-hidden after {Count} unique reports",
                        request.EntityId, effectiveCount);
                }
            }
        }

        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Report {ReportId} submitted by {UserId} against {EntityType}:{EntityId}",
            report.Id, request.ReporterUserId, request.EntityType, request.EntityId);

        return Result.Success(report.Id);
    }
}
