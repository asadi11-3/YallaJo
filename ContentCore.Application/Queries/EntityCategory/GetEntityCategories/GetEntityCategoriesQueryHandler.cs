using ContentCore.Domain.Enums;
using ContentCore.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Queries.EntityCategory.GetEntityCategories;

public sealed class GetEntityCategoriesQueryHandler(
    IEntityCategoryRepository entityCategoryRepository,
    ILogger<GetEntityCategoriesQueryHandler> logger)
    : IQueryHandler<GetEntityCategoriesQuery, IReadOnlyList<EntityCategoryDto>>
{
    public async Task<Result<IReadOnlyList<EntityCategoryDto>>> Handle(
        GetEntityCategoriesQuery request,
        CancellationToken ct)
    {
        try
        {
            if (!Enum.TryParse<EntityType>(request.EntityType, true, out var entityType))
            {
                return Result<IReadOnlyList<EntityCategoryDto>>.Failure(
                   new Error("EntityCategory.InvalidEntityType", "Invalid entity type."),
                   Outcome.Invalid);
            }

            var entityCategories = await entityCategoryRepository.GetByEntityAsync(entityType, request.EntityId, ct);
            var dtos = entityCategories
                .Select(ec => new EntityCategoryDto(ec.CategoryId, ec.Category.Name, ec.Category.Slug))
                .ToList() as IReadOnlyList<EntityCategoryDto>;

            logger.LogDebug(
                "GetEntityCategories: {Count} categories for {EntityType}/{EntityId}",
                dtos.Count, request.EntityType, request.EntityId);

            return Result<IReadOnlyList<EntityCategoryDto>>.Success(dtos);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result<IReadOnlyList<EntityCategoryDto>>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
