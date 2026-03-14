using ContentCore.Domain.Enums;
using ContentCore.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Commands.EntityTag.RemoveTagFromEntity;

public sealed class RemoveTagFromEntityCommandHandler(
    IEntityTagRepository entityTagRepository,
    IContentCoreUnitOfWork unitOfWork)
    : ICommandHandler<RemoveTagFromEntityCommand>
{
    public async Task<Result> Handle(RemoveTagFromEntityCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<EntityType>(request.EntityType, true, out var entityType))
            return Result.Invalid(new Error("InvalidEntityType", "Invalid entity type."));

        var entityTags = await entityTagRepository.GetByEntityAsync(entityType, request.EntityId, ct);
        var entityTag = entityTags.FirstOrDefault(et => et.TagId == request.TagId);
        if (entityTag is null)
            return Result.NotFound(
                $"Tag '{request.TagId}' is not assigned to {request.EntityType}/{request.EntityId}.");

        entityTagRepository.Remove(entityTag);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
