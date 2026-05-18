using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Blog.CreateBlog;

public sealed record CreateBlogCommand(
    string Title,
    string Content,
    string SourceLanguageCode,
    string? Slug = null,
    string? Summary = null,
    string? MetaTitle = null,
    string? MetaDescription = null,
    Guid? PlaceId = null) : ICommand<CreateBlogResult>;
