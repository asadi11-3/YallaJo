using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Creator.RequestMoreInfo;

public sealed record RequestMoreInfoCommand(
    Guid ApplicationId,
    string AdminNote) : ICommand;
