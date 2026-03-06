using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Language.CreateLanguage;

public sealed record CreateLanguageResult(Guid Id, string Code, string Name);

public sealed record CreateLanguageCommand(
    string Code,
    string Name,
    string NativeName,
    bool IsRtl) : ICommand<CreateLanguageResult>;
