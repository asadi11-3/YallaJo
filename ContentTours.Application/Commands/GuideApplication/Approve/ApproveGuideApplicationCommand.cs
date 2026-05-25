using YallaJo.SharedKernel.Application.Abstractions.Messaging;
namespace ContentTours.Application.Commands.GuideApplication.Approve;

public sealed record ApproveGuideApplicationCommand(Guid ApplicationId) : ICommand;
