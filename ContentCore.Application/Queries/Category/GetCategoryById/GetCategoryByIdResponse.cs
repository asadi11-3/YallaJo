using ContentCore.Application.Queries.Category.Common;

namespace ContentCore.Application.Queries.Category.GetCategoryById;

public sealed record GetCategoryByIdResponse(
    CategoryTreeDto Category);