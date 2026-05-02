namespace ContentTours.Application.Interfaces;

/// <summary>
/// Cross-module contract for fetching minimal public-facing profile data
/// (display name, avatar) by userId. The canonical implementation will
/// live in the Accounts module; ContentTours uses a NoOp stub until that
/// integration lands (per Phase C deferral).
/// </summary>
/// <remarks>
/// Used by GetTourGuidesQueryHandler to enrich the tour guides DTO with
/// human-friendly display data. Consumers MUST tolerate null returns
/// and fall back to userId.ToString() for displayName.
/// </remarks>
public interface IProfileLookupService
{
    Task<PublicProfile?> GetPublicProfileAsync(
        Guid userId,
        CancellationToken cancellationToken);
}

public sealed record PublicProfile(
    Guid UserId,
    string DisplayName,
    string? AvatarUrl);
