using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Blog.UnfeatureBlog;

public sealed record UnfeatureBlogCommand(Guid BlogId, byte[] RowVersion) : ICommand;
