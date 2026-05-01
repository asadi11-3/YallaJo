using ContentTours.Application.Commands.TourGuides.Assign;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Entities;
using ContentTours.Domain.Repositories;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Features.TourGuides.Commands.Assign;

internal sealed class AssignTourGuideCommandHandler : ICommandHandler<AssignTourGuideCommand>
{
    private readonly ITourTourGuideRepository _guideRepository;
    private readonly IContentToursUnitOfWork _unitOfWork;

    public AssignTourGuideCommandHandler(ITourTourGuideRepository guideRepository, IContentToursUnitOfWork unitOfWork)
    {
        _guideRepository = guideRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(AssignTourGuideCommand request, CancellationToken cancellationToken)
    {
        var existingGuide = await _guideRepository.GetAsync(request.TourId, request.TourGuideUserId, cancellationToken);
        if (existingGuide is not null) return Result.Failure(new Error("TourGuide.AlreadyAssigned", "Guide is already assigned."));

        if (request.IsPrimary)
        {
            var allGuides = await _guideRepository.GetByTourIdAsync(request.TourId, cancellationToken);
            var currentPrimary = allGuides.FirstOrDefault(g => g.IsPrimary);
            if (currentPrimary is not null) currentPrimary.SetAsNonPrimary();
        }

        var newGuide = TourTourGuide.Create(request.TourId, request.TourGuideUserId, request.IsPrimary);
        _guideRepository.Add(newGuide);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
