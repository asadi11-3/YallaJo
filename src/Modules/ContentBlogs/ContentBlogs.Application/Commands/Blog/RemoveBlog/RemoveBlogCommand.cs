using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Blog.RemoveBlog;

public sealed record RemoveBlogCommand(Guid BlogId, byte[] RowVersion, string Reason) : ICommand;
