using YallaJo.SharedKernel.Application.Abstractions.Messaging;
namespace ContentTours.Application.Commands.GuideApplication.Reject;

public sealed record RejectGuideApplicationCommand(Guid ApplicationId, string Reason) : ICommand;
