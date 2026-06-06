namespace ContentBlogs.Application.Queries.Creator.Dtos;

/// <summary>
/// Public-safe follower projection for the anonymous followers endpoint
/// (Creator Backend Contract Polish, Gap 3 Phase A).
/// <para>
/// Deliberately exposes NO follower identity — no user id, no display name, no avatar.
/// Only an opaque page-aware <see cref="Ordinal"/> and the <see cref="FollowedAt"/>
/// timestamp. This removes the previous raw-Guid leak while deferring identity
/// enrichment (Phase B) to a separate batch with a dedicated Accounts reader.
/// </para>
/// </summary>
public sealed record FollowerSummaryDto(
    int Ordinal,
    DateTime FollowedAt);
