using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Creator.AdminUpdate;

public sealed record AdminUpdateCreatorProfileCommand(
    Guid ProfileId,
    string DisplayName,
    string? Bio,
    string? AvatarUrl,
    string? Slug) : ICommand;
