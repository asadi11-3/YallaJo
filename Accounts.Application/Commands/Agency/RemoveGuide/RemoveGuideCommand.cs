using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Commands.Agency.RemoveGuide;

public sealed record RemoveGuideCommand(Guid GuideUserId, string Reason) : ICommand;
