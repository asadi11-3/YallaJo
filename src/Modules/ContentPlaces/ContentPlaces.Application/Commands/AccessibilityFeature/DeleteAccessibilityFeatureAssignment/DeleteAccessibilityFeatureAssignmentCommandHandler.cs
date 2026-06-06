using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Commands.AccessibilityFeature.DeleteAccessibilityFeatureAssignment;

public sealed class DeleteAccessibilityFeatureAssignmentCommandHandler(
    IAccessibilityFeatureRepository featureRepository,
    IContentPlacesUnitOfWork unitOfWork,
    ILogger<DeleteAccessibilityFeatureAssignmentCommandHandler> logger)
    : ICommandHandler<DeleteAccessibilityFeatureAssignmentCommand>
{
    public async Task<Result> Handle(
        DeleteAccessibilityFeatureAssignmentCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await featureRepository.GetByIdAsync(request.AssignmentId, cancellationToken);
        if (existing is null)
        {
            return Result.Failure(
                new Error("AccessibilityFeature.NotFound",
                    $"Accessibility-feature assignment '{request.AssignmentId}' was not found."),
                Outcome.NotFound);
        }

        featureRepository.Remove(existing);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Admin deleted accessibility feature assignment {AssignmentId} (EntityType {EntityType}, EntityId {EntityId})",
            existing.Id, existing.EntityType, existing.EntityId);

        return Result.Success();
    }
}
