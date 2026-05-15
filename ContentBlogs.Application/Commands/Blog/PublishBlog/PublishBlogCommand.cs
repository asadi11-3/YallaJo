using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Blog.PublishBlog;

public sealed record PublishBlogCommand(Guid BlogId) : ICommand;
