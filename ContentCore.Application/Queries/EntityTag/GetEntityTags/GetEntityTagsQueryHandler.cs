using ContentCore.Domain.Enums;
using ContentCore.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Queries.EntityTag.GetEntityTags;

public sealed class GetEntityTagsQueryHandler(IEntityTagRepository entityTagRepository)
    : IQueryHandler<GetEntityTagsQuery, IReadOnlyList<EntityTagDto>>
{
    public async Task<Result<IReadOnlyList<EntityTagDto>>> Handle(
        GetEntityTagsQuery request,
        CancellationToken ct)
    {
        try
        {
            if (!Enum.TryParse<EntityType>(request.EntityType, true, out var entityType))
                return Result<IReadOnlyList<EntityTagDto>>.Success(Array.Empty<EntityTagDto>());

            var entityTags = await entityTagRepository.GetByEntityAsync(entityType, request.EntityId, ct);
            var dtos = entityTags
                .Select(et => new EntityTagDto(et.TagId, et.Tag.Name, et.Tag.Slug))
                .ToList() as IReadOnlyList<EntityTagDto>;

            return Result<IReadOnlyList<EntityTagDto>>.Success(dtos);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result<IReadOnlyList<EntityTagDto>>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
