using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Tag.ActivateTag;

public sealed record ActivateTagCommand(Guid Id) : ICommand;
