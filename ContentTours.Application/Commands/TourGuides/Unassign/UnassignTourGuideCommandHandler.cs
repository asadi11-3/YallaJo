using ContentTours.Application.Commands.TourGuides.Unassign;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Repositories;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Features.TourGuides.Commands.Unassign;

internal sealed class UnassignTourGuideCommandHandler : ICommandHandler<UnassignTourGuideCommand>
{
    private readonly ITourTourGuideRepository _guideRepository;
    private readonly IContentToursUnitOfWork _unitOfWork;

    public UnassignTourGuideCommandHandler(ITourTourGuideRepository guideRepository, IContentToursUnitOfWork unitOfWork)
    {
        _guideRepository = guideRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UnassignTourGuideCommand request, CancellationToken cancellationToken)
    {
        var guideToRemove = await _guideRepository.GetAsync(request.TourId, request.TourGuideUserId, cancellationToken);
        if (guideToRemove is null) return Result.Failure(new Error("TourGuide.NotFound", "Guide not found."));

        _guideRepository.Remove(guideToRemove);

        if (guideToRemove.IsPrimary)
        {
            var allGuides = await _guideRepository.GetByTourIdAsync(request.TourId, cancellationToken);
            var backupGuide = allGuides.FirstOrDefault(g => g.TourGuideId != request.TourGuideUserId);
            if (backupGuide is not null) backupGuide.SetAsPrimary();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
