using ContentTours.Domain.Enums;
using ContentTours.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Queries.TourGuides.LookupAssignableGuides;

/// <summary>
/// [Backend] B7 — active guide profiles matching the term, excluding guides already
/// assigned to the tour. Endpoint-level permission (TourGuide.Update) gates access.
/// </summary>
public sealed class LookupAssignableGuidesQueryHandler(
    ITourGuideRepository guideProfileRepository,
    ITourTourGuideRepository assignmentRepository,
    ILogger<LookupAssignableGuidesQueryHandler> logger)
    : IQueryHandler<LookupAssignableGuidesQuery, IReadOnlyList<GuideLookupDto>>
{
    private const int MinPageSize = 1;
    private const int MaxPageSize = 10;

    public async Task<Result<IReadOnlyList<GuideLookupDto>>> Handle(
        LookupAssignableGuidesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var pageSize = Math.Clamp(request.PageSize, MinPageSize, MaxPageSize);
            var term = request.Term?.Trim();

            var assignedUserIds = await assignmentRepository.Query(asNoTracking: true)
                .Where(a => a.TourId == request.TourId)
                .Select(a => a.TourGuideId)
                .ToListAsync(cancellationToken);

            var q = guideProfileRepository.Query(asNoTracking: true)
                .Where(g => g.Status == TourGuideStatus.Active
                            && !assignedUserIds.Contains(g.UserId));

            if (!string.IsNullOrEmpty(term))
            {
                q = q.Where(g => g.DisplayName.Contains(term));
            }

            var items = await q
                .OrderBy(g => g.DisplayName)
                .Take(pageSize)
                .Select(g => new GuideLookupDto(g.UserId, g.DisplayName, g.AvatarUrl))
                .ToListAsync(cancellationToken);

            logger.LogDebug(
                "Guide lookup for TourId={TourId} term='{Term}' returned {Count} rows",
                request.TourId, term, items.Count);

            return Result.Success<IReadOnlyList<GuideLookupDto>>(items);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<IReadOnlyList<GuideLookupDto>>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
