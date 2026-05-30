using ContentBlogs.Application.Queries.Blog.Dtos;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Queries.Blog.GetAdminBlogById;

public sealed record GetAdminBlogByIdQuery(Guid BlogId, string? AcceptLanguage = null)
    : IQuery<AdminBlogDetailDto>;
