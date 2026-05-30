using Accounts.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Commands.Provider.RegisterProvider;

public sealed record RegisterProviderCommand(
    ProviderType Type,
    string BusinessName,
    string ContactEmail,
    string ContactPhone,
    string Address,
    string Description,
    string? TypeSpecificDataJson) : ICommand<RegisterProviderResult>;
