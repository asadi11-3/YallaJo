using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Blog.UnhideBlog;

public sealed record UnhideBlogCommand(Guid BlogId, byte[] RowVersion) : ICommand;
