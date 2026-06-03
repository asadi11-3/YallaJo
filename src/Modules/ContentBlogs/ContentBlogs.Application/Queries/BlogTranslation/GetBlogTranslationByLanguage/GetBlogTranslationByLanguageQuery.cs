using ContentBlogs.Application.Queries.BlogTranslation.Dtos;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Queries.BlogTranslation.GetBlogTranslationByLanguage;

public sealed record GetBlogTranslationByLanguageQuery(Guid BlogId, string LanguageCode)
    : IQuery<BlogTranslationAdminDto>;
