using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Tag.UpdateTag;

public sealed record UpdateTagCommand(
    Guid Id,
    string Name,
    string Slug,
    string SourceLanguageCode = "en") : ICommand<UpdateTagResult>;
