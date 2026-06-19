using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.TourGuides.ClearAvatar;

/// <summary>Clears the current user's tour-guide avatar and best-effort deletes the old local file.</summary>
public sealed record ClearGuideAvatarCommand : ICommand;
