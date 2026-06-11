namespace ContentBlogs.Application.Queries.Creator.Dtos;

/// <summary>
/// Per-status creator application counts for the admin queue's counted tabs.
/// </summary>
public sealed record CreatorApplicationStatusCountsDto(
    int Draft,
    int Pending,
    int Approved,
    int Rejected,
    int MoreInfoNeeded);
