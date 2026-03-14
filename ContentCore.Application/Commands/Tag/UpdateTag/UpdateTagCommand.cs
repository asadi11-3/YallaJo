using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Tag.UpdateTag;

public sealed record UpdateTagResult(Guid Id, string Name, string Slug);

public sealed record UpdateTagCommand(Guid Id, string Name, string Slug) : ICommand<UpdateTagResult>;
