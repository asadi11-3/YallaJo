using ContentCore.Application.Queries.Category.ListCategories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Queries.Category.GetCategoryById;

/// <param name="Id">Category ID to retrieve.</param>
/// <param name="WithTranslations">
/// When true, all available translations are included in the response.
/// Set by the endpoint when the client sends an Accept-Language header.
/// </param>
public sealed record GetCategoryByIdQuery(
    Guid Id,
    bool WithTranslations = false) : IQuery<CategoryDto>;
