using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ContentTours.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Features.TourGuides.Queries.GetByTourId;

internal sealed class GetTourGuidesQueryHandler : IQueryHandler<GetTourGuidesQuery, IReadOnlyCollection<TourGuideResponse>>
{
    private readonly ITourTourGuideRepository _guideRepository;

    public GetTourGuidesQueryHandler(ITourTourGuideRepository guideRepository)
    {
        _guideRepository = guideRepository;
    }

    public async Task<Result<IReadOnlyCollection<TourGuideResponse>>> Handle(GetTourGuidesQuery request, CancellationToken cancellationToken)
    {
        var guides = await _guideRepository.GetByTourIdAsync(request.TourId, cancellationToken);

        var response = guides.OrderByDescending(g => g.IsPrimary)
                             .Select(g => new TourGuideResponse(g.TourGuideId, g.IsPrimary))
                             .ToList();

        return Result.Success<IReadOnlyCollection<TourGuideResponse>>(response);
    }
}
