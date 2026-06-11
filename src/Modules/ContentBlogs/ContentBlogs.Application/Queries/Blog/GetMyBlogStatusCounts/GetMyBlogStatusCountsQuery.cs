using ContentBlogs.Application.Queries.Blog.Dtos;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Queries.Blog.GetMyBlogStatusCounts;

/// <summary>
/// Returns aggregate counts of the caller's own blogs grouped by lifecycle bucket
/// (draft, pending review, published, archived, deleted). Owner-scoped; no parameters.
/// </summary>
public sealed record GetMyBlogStatusCountsQuery : IQuery<MyBlogStatusCountsDto>;
