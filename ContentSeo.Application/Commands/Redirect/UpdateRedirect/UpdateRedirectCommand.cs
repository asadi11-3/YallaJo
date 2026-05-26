using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentSeo.Application.Commands.Redirect.UpdateRedirect;

public sealed record UpdateRedirectCommand(
    Guid Id,
    string? NewUrl,
    int? StatusCode,
    bool? IsActive) : ICommand;
