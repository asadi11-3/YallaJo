using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Blog.MarkBlogAsFeatured;

public sealed record MarkBlogAsFeaturedCommand(Guid BlogId, byte[] RowVersion) : ICommand;
