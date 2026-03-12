using ContentCore.Application.Queries.Category.Common;

namespace ContentCore.Application.Queries.Category.GetCategories;

public sealed record GetCategoriesResponse(
    IReadOnlyList<CategoryTreeDto> Categories
);