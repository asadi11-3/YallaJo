using ContentBlogs.Application.Queries.Blog.Dtos;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;

namespace ContentBlogs.Application.Queries.Blog.GetMyBlogs;

public sealed record GetMyBlogsQuery(
    int Page = 1,
    int PageSize = 20,
    string? StatusFilter = null,
    string? AcceptLanguage = null)
    : IQuery<PaginatedResult<BlogSummaryDto>>;
