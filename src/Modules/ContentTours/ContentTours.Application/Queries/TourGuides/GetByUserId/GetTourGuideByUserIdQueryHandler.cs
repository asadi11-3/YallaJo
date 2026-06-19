using ContentTours.Application.Queries.TourGuides.Common;
using ContentTours.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Queries.TourGuides.GetByUserId;

/// <summary>
/// Handler for <see cref="GetTourGuideByUserIdQuery"/>. Mirrors
/// <c>GetTourGuideByIdQueryHandler</c> but looks up the aggregate by
/// <c>TourGuide.UserId</c> instead of the aggregate <c>Id</c>.
/// </summary>
public sealed class GetTourGuideByUserIdQueryHandler(
    ITourGuideRepository guideRepository,
    ILogger<GetTourGuideByUserIdQueryHandler> logger)
    : IQueryHandler<GetTourGuideByUserIdQuery, TourGuideProfileDto>
{
    public async Task<Result<TourGuideProfileDto>> Handle(
        GetTourGuideByUserIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var guide = await guideRepository
                .GetWithDetailsByUserIdAsync(request.UserId, cancellationToken)
                .ConfigureAwait(false);

            if (guide is null)
            {
                return Result<TourGuideProfileDto>.Failure(
                    new Error("TourGuide.NotFound", $"Tour guide for user '{request.UserId}' was not found."),
                    Outcome.NotFound);
            }

            var tourCount = await guideRepository
                .CountAssignedToursAsync(guide.UserId, cancellationToken)
                .ConfigureAwait(false);

            var dto = new TourGuideProfileDto(
                Id: guide.Id,
                UserId: guide.UserId,
                DisplayName: guide.DisplayName,
                AvatarUrl: guide.AvatarUrl,
                Bio: guide.Bio,
                YearsOfExperience: guide.YearsOfExperience,
                HasFirstAid: guide.HasFirstAid,
                MoTALicenseNumber: guide.MoTALicenseNumber,
                AverageRating: guide.AverageRating,
                ReviewCount: guide.ReviewCount,
                TourCount: tourCount,
                Languages: guide.Languages
                    .OrderBy(language => language.LanguageId)
                    .Select(language => new TourGuideLanguageDto(language.LanguageId, language.Proficiency))
                    .ToList(),
                Specializations: guide.Specializations
                    .OrderBy(specialization => specialization.SpecializationId)
                    .Select(specialization => new TourGuideSpecializationDto(specialization.SpecializationId))
                    .ToList());

            logger.LogDebug("Resolved TourGuide profile {TourGuideId} via UserId {UserId}.", guide.Id, guide.UserId);

            return Result.Success(dto);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<TourGuideProfileDto>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
