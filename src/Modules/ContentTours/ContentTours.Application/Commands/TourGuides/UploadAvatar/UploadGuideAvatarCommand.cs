using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.TourGuides.UploadAvatar;

/// <summary>
/// Persists a previously stored managed avatar URL onto the current user's tour-guide profile.
/// The URL is produced by the managed upload endpoint (local <c>guides/avatars</c> folder) — it is
/// never an arbitrary client-supplied URL.
/// </summary>
public sealed record UploadGuideAvatarCommand(string AvatarUrl) : ICommand;
