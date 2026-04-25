using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.ProvisionAccount;

public sealed record ProvisionAccountCommand(
    string Email,
    string FirstName,
    string LastName,
    string? DisplayName,
    string? AvatarUrl,
    IReadOnlyList<Guid> InitialRoleIds) : ICommand<ProvisionAccountResult>;
