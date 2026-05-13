namespace ContentTours.Application.Interfaces;

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
