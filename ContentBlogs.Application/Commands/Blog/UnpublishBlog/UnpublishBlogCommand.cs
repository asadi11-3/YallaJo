using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Blog.UnpublishBlog;

public sealed record UnpublishBlogCommand(Guid BlogId) : ICommand;
