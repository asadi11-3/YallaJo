using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Tag.DeleteTag;

public sealed record DeleteTagCommand(Guid Id) : ICommand;
