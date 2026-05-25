using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Commands.Agency.RejectGuideApplication;

public sealed record RejectGuideApplicationCommand(Guid ApplicationId, string Reason) : ICommand;
