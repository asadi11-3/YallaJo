using ContentTours.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Queries.TourGuides.GetMyApplications;

internal sealed class GetMyGuideApplicationsQueryHandler(
    IGuideApplicationRepository applicationRepository,
    ITourRepository tourRepository,
    ILogger<GetMyGuideApplicationsQueryHandler> logger)
    : IQueryHandler<GetMyGuideApplicationsQuery, GetMyGuideApplicationsResult>
{
    public async Task<Result<GetMyGuideApplicationsResult>> Handle(
        GetMyGuideApplicationsQuery request,
        CancellationToken cancellationToken)
    {
        var allApplications = await applicationRepository
            .GetByGuideIdAsync(request.TourGuideId, ct: cancellationToken)
            .ConfigureAwait(false);

        var totalCount = allApplications.Count;

        var paged = allApplications
            .OrderByDescending(a => a.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var items = new List<GuideApplicationListItemDto>(paged.Count);

        foreach (var app in paged)
        {
            var tour = await tourRepository
                .GetByIdAsync(app.TourId, cancellationToken)
                .ConfigureAwait(false);

            items.Add(new GuideApplicationListItemDto(
                ApplicationId: app.Id,
                TourId: app.TourId,
                TourTitle: tour?.Name,
                Status: app.Status.ToString(),
                Message: app.Message,
                ProposedBasePrice: app.ProposedBasePrice,
                CreatedAt: app.CreatedAt,
                ReviewedAt: app.ReviewedAt,
                RejectionReason: app.RejectionReason));
        }

        logger.LogDebug("Listed {Count}/{Total} applications for guide {GuideId}",
            items.Count, totalCount, request.TourGuideId);

        return Result.Success(new GetMyGuideApplicationsResult(items, totalCount));
    }
}
