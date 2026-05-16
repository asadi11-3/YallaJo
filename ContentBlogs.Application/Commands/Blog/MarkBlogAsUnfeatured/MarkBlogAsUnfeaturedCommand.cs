using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Blog.MarkBlogAsUnfeatured;

public sealed record MarkBlogAsUnfeaturedCommand(Guid BlogId, byte[] RowVersion) : ICommand;
