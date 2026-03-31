using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Language.CreateLanguage;

public sealed record CreateLanguageCommand(
    string Code,
    string Name,
    string NativeName,
    bool IsRtl) : ICommand<CreateLanguageResult>;
