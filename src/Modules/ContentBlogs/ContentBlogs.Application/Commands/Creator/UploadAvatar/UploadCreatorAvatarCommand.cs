using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Creator.UploadAvatar;

/// <summary>
/// Sets the current creator's avatar to an already-uploaded, validated public URL.
/// The physical upload + content validation happen in the presentation layer;
/// this command persists the URL and best-effort cleans up the previously stored local avatar.
/// </summary>
public sealed record UploadCreatorAvatarCommand(string AvatarUrl) : ICommand;
