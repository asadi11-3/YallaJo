using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Blog.UpdateBlog;

public sealed record UpdateBlogCommand(
    Guid BlogId,
    string Title,
    string Slug,
    string Content,
    string? Summary = null,
    string? MetaTitle = null,
    string? MetaDescription = null,
    Guid? PlaceId = null,
    int? ReadTimeMinutes = null) : ICommand;
