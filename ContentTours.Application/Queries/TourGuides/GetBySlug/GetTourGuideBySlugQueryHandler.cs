using ContentTours.Application.Interfaces;
using ContentTours.Application.Queries.TourGuides.Common;
using ContentTours.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Queries.TourGuides.GetBySlug;

internal sealed class GetTourGuideBySlugQueryHandler(
    ITourGuideRepository guideRepository,
    IProfileLookupService profileLookup,
    ILogger<GetTourGuideBySlugQueryHandler> logger)
    : IQueryHandler<GetTourGuideBySlugQuery, TourGuideProfileDto>
{
    public async Task<Result<TourGuideProfileDto>> Handle(
        GetTourGuideBySlugQuery request,
        CancellationToken cancellationToken)
    {
        var guide = await guideRepository
            .GetBySlugAsync(request.Slug, cancellationToken)
            .ConfigureAwait(false);

        if (guide is null)
        {
            return Result<TourGuideProfileDto>.Failure(
                new Error("TourGuide.NotFound", $"Tour guide with slug '{request.Slug}' was not found."),
                Outcome.NotFound);
        }

        var publicProfile = await profileLookup
            .GetPublicProfileAsync(guide.UserId, cancellationToken)
            .ConfigureAwait(false);

        var tourCount = await guideRepository
            .CountAssignedToursAsync(guide.UserId, cancellationToken)
            .ConfigureAwait(false);

        var dto = new TourGuideProfileDto(
            Id: guide.Id,
            UserId: guide.UserId,
            DisplayName: publicProfile?.DisplayName,
            AvatarUrl: publicProfile?.AvatarUrl,
            Bio: guide.Bio,
            YearsOfExperience: guide.YearsOfExperience,
            HasFirstAid: guide.HasFirstAid,
            MoTALicenseNumber: guide.MoTALicenseNumber,
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
                .ToList());

        logger.LogDebug("Found TourGuide by slug {Slug}: {GuideId}", request.Slug, guide.Id);

        return Result.Success(dto);
    }
}
