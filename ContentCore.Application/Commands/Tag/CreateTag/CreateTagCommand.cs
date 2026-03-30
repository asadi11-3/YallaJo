using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Tag.CreateTag;

public sealed record CreateTagCommand(string Name, string Slug) : ICommand<CreateTagResult>;
