namespace ContentCore.Contracts.Attachments;

public sealed record EntityImageDto(
    string Url,
    string? ThumbnailUrl,
    int SortOrder,
    bool IsPrimary);
