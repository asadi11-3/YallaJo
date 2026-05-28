using ContentBlogs.Application.Queries.Blog.Dtos;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;

namespace ContentBlogs.Application.Queries.Blog.GetCreatorBlogsBySlug;

/// <summary>Returns published blogs for a creator identified by slug (public endpoint).</summary>
public sealed record GetCreatorBlogsBySlugQuery(
    string CreatorSlug,
    int Page = 1,
    int PageSize = 20)
    : IQuery<PaginatedResult<BlogSummaryDto>>;
