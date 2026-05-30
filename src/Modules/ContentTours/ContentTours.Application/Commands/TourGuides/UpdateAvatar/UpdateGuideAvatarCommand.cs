using YallaJo.SharedKernel.Application.Abstractions.Messaging;
namespace ContentTours.Application.Commands.TourGuides.UpdateAvatar;

public sealed record UpdateGuideAvatarCommand(string AvatarUrl) : ICommand;
