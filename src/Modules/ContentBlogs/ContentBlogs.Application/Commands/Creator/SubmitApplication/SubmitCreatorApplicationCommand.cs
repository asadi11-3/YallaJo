using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Creator.SubmitApplication;

public sealed record SubmitCreatorApplicationCommand(Guid ApplicationId) : ICommand;
