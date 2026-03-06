using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Language.UpdateLanguage;

public sealed record UpdateLanguageResult(Guid Id, string Name, string NativeName, bool IsRtl, bool IsActive);

public sealed record UpdateLanguageCommand(
    Guid Id,
    string Name,
    string NativeName,
    bool IsRtl,
    bool IsActive) : ICommand<UpdateLanguageResult>;
