using ContentBlogs.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Creator.Posts.CreatePost;

public sealed record CreateCreatorPostCommand(
    CreatorPostType PostType,
    string Title,
    string Excerpt,
    Guid LanguageId,
    string? TypeSpecificDataJson,
    List<Guid>? NicheIds,
    List<string>? FreeTags) : ICommand<CreateCreatorPostResult>;

public sealed record CreateCreatorPostResult(Guid PostId, string Slug);
