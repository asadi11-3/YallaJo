namespace YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Attachments.Responses;

public sealed class AttachmentItemResponse
{
    public Guid Id { get; init; }
    public string EntityType { get; init; } = string.Empty;
    public Guid EntityId { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string? ThumbnailUrl { get; init; }
    public string? OriginalFileName { get; init; }
    public string? MimeType { get; init; }
    public long? FileSize { get; init; }
    public int? Width { get; init; }
    public int? Height { get; init; }
    public int? DurationSeconds { get; init; }
    public int SortOrder { get; init; }
    public DateTime UploadedAt { get; init; }
    public Guid UploadedByUserId { get; init; }
}
