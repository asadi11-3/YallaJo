using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Blog.ApproveBlog;

public sealed record ApproveBlogCommand(Guid BlogId, byte[] RowVersion) : ICommand;
