using ContentTours.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Queries.TourGuides.ListApplications;

internal sealed class ListGuideApplicationsQueryHandler(
    IGuideApplicationRepository applicationRepository,
    ITourRepository tourRepository,
    ILogger<ListGuideApplicationsQueryHandler> logger)
    : IQueryHandler<ListGuideApplicationsQuery, ListGuideApplicationsResult>
{
    public async Task<Result<ListGuideApplicationsResult>> Handle(
        ListGuideApplicationsQuery request,
        CancellationToken cancellationToken)
    {
        var allApplications = await applicationRepository
            .GetByTourIdAsync(request.TourId, request.Status, ct: cancellationToken)
            .ConfigureAwait(false);

        var totalCount = allApplications.Count;

        var tour = await tourRepository
            .GetByIdAsync(request.TourId, cancellationToken)
            .ConfigureAwait(false);

        var items = allApplications
            .OrderByDescending(a => a.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(app => new GuideApplicationAdminListItemDto(
                ApplicationId: app.Id,
                TourId: app.TourId,
                TourTitle: tour?.Name,
                TourGuideId: app.TourGuideId,
                GuideUserId: app.GuideUserId,
                Status: app.Status.ToString(),
                Message: app.Message,
                RelevantExperience: app.RelevantExperience,
                ProposedBasePrice: app.ProposedBasePrice,
                ResubmissionCount: app.ResubmissionCount,
                CreatedAt: app.CreatedAt,
                ReviewedAt: app.ReviewedAt,
                ReviewedByAdminId: app.ReviewedByAdminId,
                RejectionReason: app.RejectionReason))
            .ToList();

        logger.LogDebug("Listed {Count}/{Total} guide applications for tour {TourId}",
            items.Count, totalCount, request.TourId);

        return Result.Success(new ListGuideApplicationsResult(items, totalCount));
    }
}
