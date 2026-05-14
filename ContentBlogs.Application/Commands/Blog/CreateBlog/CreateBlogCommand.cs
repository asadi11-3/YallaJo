using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Blog.CreateBlog;

public record CreateBlogCommand : ICommand<CreateBlogResult>
{
    public string Title { get; init; } = default!;

    public string Slug { get; init; } = default!;

    public string Content { get; init; } = default!;

    public string? Summary { get; init; }

    public Guid? PlaceId { get; init; }

    public string MetaTitle { get; init; } = default!;

    public string MetaDescription { get; init; } = default!;

    public string SourceLanguageCode { get; init; } = default!;
}
