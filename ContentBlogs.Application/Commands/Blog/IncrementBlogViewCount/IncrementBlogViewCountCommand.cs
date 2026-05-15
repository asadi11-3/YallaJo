using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Blog.IncrementBlogViewCount;

/// <summary>
/// Atomically increments <c>Blog.ViewCount</c> for a Published blog.
/// Public, unauthenticated traffic — no <c>RowVersion</c> required because the
/// underlying SQL is a single-statement increment that does not raise domain
/// events or write outbox messages.
/// </summary>
public sealed record IncrementBlogViewCountCommand(Guid BlogId) : ICommand;
