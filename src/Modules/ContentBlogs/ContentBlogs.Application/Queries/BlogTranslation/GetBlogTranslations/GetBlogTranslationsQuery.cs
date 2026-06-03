using ContentBlogs.Application.Queries.BlogTranslation.Dtos;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Queries.BlogTranslation.GetBlogTranslations;

public sealed record GetBlogTranslationsQuery(Guid BlogId)
    : IQuery<IReadOnlyList<BlogTranslationAdminDto>>;
