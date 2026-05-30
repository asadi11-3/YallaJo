using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Blog.FeatureBlog;

public sealed record FeatureBlogCommand(Guid BlogId, byte[] RowVersion, DateTime? FeaturedUntil = null) : ICommand;
