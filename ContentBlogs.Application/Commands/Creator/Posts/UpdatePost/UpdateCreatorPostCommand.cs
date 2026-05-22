using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Creator.Posts.UpdatePost;

public sealed record UpdateCreatorPostCommand(
    Guid PostId,
    string Title,
    string Excerpt,
    string? Body,
    string? TypeSpecificDataJson) : ICommand;
