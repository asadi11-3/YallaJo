using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Creator.ClearAvatar;

/// <summary>
/// Clears the current creator's avatar (sets <c>AvatarUrl</c> to null) and best-effort
/// deletes the previously stored local avatar file.
/// </summary>
public sealed record ClearCreatorAvatarCommand : ICommand;
