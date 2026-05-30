using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Tag.DeactivateTag;

public sealed record DeactivateTagCommand(Guid Id) : ICommand;
