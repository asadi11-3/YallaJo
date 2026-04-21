using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Contracts.Abstractions;

/// <summary>
/// Accounts-owned capability exposed to other modules (e.g. Auth) to create
/// the profile row linked to a newly created/invited Security user. Keeps the
/// <c>Profile</c> aggregate inside the Accounts module — callers pass plain
/// data and receive the resulting <c>ProfileId</c>.
/// </summary>
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
