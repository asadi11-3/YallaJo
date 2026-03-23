using ContentCore.Domain.Enums;
using ContentCore.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Queries.EntityCategory.GetEntityCategories;

public sealed class GetEntityCategoriesQueryHandler(IEntityCategoryRepository entityCategoryRepository)
    : IQueryHandler<GetEntityCategoriesQuery, IReadOnlyList<EntityCategoryDto>>
{
    public async Task<Result<IReadOnlyList<EntityCategoryDto>>> Handle(
        GetEntityCategoriesQuery request,
        CancellationToken ct)
    {
        try
        {
            if (!Enum.TryParse<EntityType>(request.EntityType, true, out var entityType))
                return Result<IReadOnlyList<EntityCategoryDto>>.Success(Array.Empty<EntityCategoryDto>());

            var entityCategories = await entityCategoryRepository.GetByEntityAsync(entityType, request.EntityId, ct);
            var dtos = entityCategories
                .Select(ec => new EntityCategoryDto(ec.CategoryId, ec.Category.Name, ec.Category.Slug))
                .ToList() as IReadOnlyList<EntityCategoryDto>;

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
