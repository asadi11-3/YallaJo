using ContentBlogs.Application.Queries.Blog.Dtos;
using ContentBlogs.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;

namespace ContentBlogs.Application.Queries.Blog.GetDeletedBlogsAdmin;

public sealed record GetDeletedBlogsAdminQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    BlogStatus? Status = null,
    Guid? PlaceId = null,
    string? SortBy = null,
    string? SortOrder = null)
    : IQuery<PaginatedResult<AdminDeletedBlogListItemDto>>;
