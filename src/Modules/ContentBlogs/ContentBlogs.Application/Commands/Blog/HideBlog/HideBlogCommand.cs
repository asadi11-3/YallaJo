using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Blog.HideBlog;

public sealed record HideBlogCommand(Guid BlogId, byte[] RowVersion, string Reason) : ICommand;
