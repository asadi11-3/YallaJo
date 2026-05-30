using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Commands.Agency.ApproveGuideApplication;

public sealed record ApproveGuideApplicationCommand(Guid ApplicationId) : ICommand;
