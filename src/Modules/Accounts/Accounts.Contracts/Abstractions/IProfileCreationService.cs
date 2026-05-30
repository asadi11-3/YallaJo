using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Contracts.Abstractions;

public interface IProfileCreationService
{
    Task<Result<Guid>> CreateForUserAsync(
        ProfileCreationRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<Guid>> CreateForInvitedUserAsync(
        InvitedProfileCreationRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record ProfileCreationRequest(
    Guid UserId,
    string FirstName,
    string LastName);

public sealed record InvitedProfileCreationRequest(
    Guid UserId,
    string FirstName,
    string LastName,
    string? DisplayName,
    string? AvatarUrl);
