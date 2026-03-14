using ContentCore.Application.Queries.Category.ListCategories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Queries.Category.GetCategoryById;

public sealed record GetCategoryByIdQuery(Guid Id) : IQuery<CategoryDto>;
