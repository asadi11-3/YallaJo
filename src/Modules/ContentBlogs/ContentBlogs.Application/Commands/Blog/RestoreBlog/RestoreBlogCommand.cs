using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Blog.RestoreBlog;

/// <summary>
/// Restores a soft-deleted Blog.  Preserves Status, PublishedAt, IsFeatured
/// (plan D1).  Subject to author-hierarchy, RowVersion, and per-PlaceId
/// featured-uniqueness re-checks (plan D2/D3).
/// </summary>
public sealed record RestoreBlogCommand(Guid BlogId, byte[] RowVersion) : ICommand;
