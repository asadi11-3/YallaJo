using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Creator.RejectApplication;

public sealed record RejectCreatorApplicationCommand(
    Guid ApplicationId,
    string Reason) : ICommand;
