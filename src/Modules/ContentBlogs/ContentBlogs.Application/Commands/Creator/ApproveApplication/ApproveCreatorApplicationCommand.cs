using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Creator.ApproveApplication;

public sealed record ApproveCreatorApplicationCommand(
    Guid ApplicationId,
    string DisplayName,
    string? AvatarUrl) : ICommand<ApproveCreatorApplicationResult>;
