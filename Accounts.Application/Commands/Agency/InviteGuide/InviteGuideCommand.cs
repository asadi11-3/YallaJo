using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Commands.Agency.InviteGuide;

public sealed record InviteGuideCommand(
    Guid GuideUserId,
    string? Message,
    decimal ProposedCommissionPercentage) : ICommand<Guid>;
