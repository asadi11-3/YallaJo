using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Accounts.Application.Commands.CreateProfile;

public sealed record CreateProfileCommand(
    Guid UserId,
    string FirstName,
    string LastName,
    string? DisplayName,
    string? AvatarUrl) : ICommand<Guid>;
