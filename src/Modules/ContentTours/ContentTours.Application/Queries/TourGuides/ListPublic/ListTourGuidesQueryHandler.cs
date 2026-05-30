using ContentTours.Application.Interfaces;
using ContentTours.Application.Queries.TourGuides.Common;
using ContentTours.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Queries.TourGuides.ListPublic;

internal sealed class ListTourGuidesQueryHandler(
    ITourGuideRepository guideRepository,
    IProfileLookupService profileLookup,
    ILogger<ListTourGuidesQueryHandler> logger)
    : IQueryHandler<ListTourGuidesQuery, ListTourGuidesResult>
{
    public async Task<Result<ListTourGuidesResult>> Handle(
        ListTourGuidesQuery request,
        CancellationToken cancellationToken)
    {
        var (guides, totalCount) = await guideRepository
            .ListActiveAsync(request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);

        var items = new List<TourGuideListItemDto>(guides.Count);

        foreach (var guide in guides)
        {
            var profile = await profileLookup
                .GetPublicProfileAsync(guide.UserId, cancellationToken)
                .ConfigureAwait(false);

            var tourCount = await guideRepository
                .CountAssignedToursAsync(guide.UserId, cancellationToken)
                .ConfigureAwait(false);

            items.Add(new TourGuideListItemDto(
                Id: guide.Id,
                UserId: guide.UserId,
                DisplayName: profile?.DisplayName,
                Slug: guide.Slug,
                AvatarUrl: profile?.AvatarUrl,
                Bio: guide.Bio,
                AverageRating: guide.AverageRating,
                ReviewCount: guide.ReviewCount,
                TourCount: tourCount,
                Languages: guide.Languages
                    .OrderBy(l => l.LanguageId)
                    .Select(l => new TourGuideLanguageDto(l.LanguageId, l.Proficiency))
                    .ToList(),
                Specializations: guide.Specializations
                    .OrderBy(s => s.SpecializationId)
                    .Select(s => new TourGuideSpecializationDto(s.SpecializationId))
                    .ToList()));
        }

        logger.LogDebug("Listed {Count}/{Total} tour guides (page {Page})", items.Count, totalCount, request.Page);

        return Result.Success(new ListTourGuidesResult(items, totalCount, request.Page, request.PageSize));
    }
}
