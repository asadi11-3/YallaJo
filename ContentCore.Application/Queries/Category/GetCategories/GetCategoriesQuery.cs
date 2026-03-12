using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Queries.Category.GetCategories;

public sealed record GetCategoriesQuery(
    Guid? ParentCategoryId,
    bool? IsActive
) : IQuery<GetCategoriesResponse>;