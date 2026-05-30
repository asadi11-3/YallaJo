namespace ContentBlogs.Application.Queries.Creator.Dtos;

public sealed record CreatorNicheDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    int SortOrder,
    bool IsActive);
