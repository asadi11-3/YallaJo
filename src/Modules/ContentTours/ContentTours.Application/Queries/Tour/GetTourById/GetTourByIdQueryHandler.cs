using ContentTours.Application.Queries.Tour.Common;
using ContentTours.Domain.Enums;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Queries.Tour.GetTourById;

public sealed class GetTourByIdQueryHandler(
    ITourRepository tourRepository,
    IActiveLanguageProvider activeLanguageProvider,
    ILogger<GetTourByIdQueryHandler> logger)
    : IQueryHandler<GetTourByIdQuery, TourDetailDto>
{
    public async Task<Result<TourDetailDto>> Handle(
        GetTourByIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var tour = await tourRepository.GetAsync(
                filter:       t => t.Id == request.Id,
                include:      q => q.Include(t => t.TourTranslations),
                asNoTracking: true,
                ct:           cancellationToken)
                .ConfigureAwait(false);

            // F45 2026-05-30: the public sees only Approved tours, but the owner
            // (creator) and admins may read a tour in any status (Draft/Pending/etc.)
            // so they can preview/manage it. Without this, a freshly created Draft
            // returned 404 to its own creator and to admins.
            var isOwner = request.RequestingUserId is { } userId && tour is not null
                && tour.CreatedByUserId == userId;
            var canViewNonPublic = request.IsPrivileged || isOwner;

            if (tour is null || (tour.Status != TourStatus.Approved && !canViewNonPublic))
            {
                return Result<TourDetailDto>.Failure(
                    new Error("Tour.NotFound", $"Tour '{request.Id}' was not found."),
                    Outcome.NotFound);
            }

            var preferredLanguageId = await AcceptLanguageResolver
                .ResolveAsync(request.AcceptLanguage, activeLanguageProvider, cancellationToken)
                .ConfigureAwait(false);

            return Result<TourDetailDto>.Success(TourDetailDto.From(tour, preferredLanguageId));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<TourDetailDto>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
