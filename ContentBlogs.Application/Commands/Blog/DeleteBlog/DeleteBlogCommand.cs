using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Blog.DeleteBlog;

public sealed record DeleteBlogCommand(Guid BlogId) : ICommand;
