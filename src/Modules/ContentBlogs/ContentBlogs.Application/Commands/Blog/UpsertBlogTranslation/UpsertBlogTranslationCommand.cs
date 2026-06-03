using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Blog.UpsertBlogTranslation;

public sealed record UpsertBlogTranslationCommand(
    Guid BlogId,
    string LanguageCode,
    string Title,
    string Content,
    string? Summary) : ICommand;
