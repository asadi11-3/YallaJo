using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Creator.UpdateProfile;

public sealed record UpdateCreatorProfileCommand(
    string? DisplayName,
    string? Bio,
    string? AvatarUrl,
    string? Slug) : ICommand;
