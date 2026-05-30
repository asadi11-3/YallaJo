using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Creator.SuspendProfile;

public sealed record SuspendCreatorProfileCommand(
    Guid ProfileId,
    string Reason) : ICommand;
