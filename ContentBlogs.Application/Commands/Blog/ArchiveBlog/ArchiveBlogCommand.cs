using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Blog.ArchiveBlog;

public sealed record ArchiveBlogCommand(Guid BlogId, byte[] RowVersion) : ICommand;
