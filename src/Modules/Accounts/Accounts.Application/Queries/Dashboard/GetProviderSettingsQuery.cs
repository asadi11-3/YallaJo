using Accounts.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Queries.Dashboard;

/// <summary>Returns editable provider settings.</summary>
public sealed record GetProviderSettingsQuery : IQuery<ProviderSettingsResult>;

public sealed record ProviderSettingsResult(
    string BusinessName,
    string ContactEmail,
    string ContactPhone,
    string Address,
    string Description,
    ProviderType ProviderType,
    string? TypeSpecificDataJson);
