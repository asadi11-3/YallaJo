using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Queries.EntityCategory.GetEntityCategories;

public sealed record EntityCategoryDto(Guid CategoryId, string Name, string Slug);

public sealed record GetEntityCategoriesQuery(string EntityType, Guid EntityId)
    : IQuery<IReadOnlyList<EntityCategoryDto>>;
