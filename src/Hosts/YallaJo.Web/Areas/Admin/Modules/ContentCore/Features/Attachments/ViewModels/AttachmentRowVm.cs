namespace YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Attachments.ViewModels;

public sealed class AttachmentRowVm
{
    public Guid Id { get; init; }
    public Guid EntityId { get; init; }
    public string EntityType { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string? ThumbnailUrl { get; init; }
    public string? OriginalFileName { get; init; }
    public int SortOrder { get; init; }
    public DateTime UploadedAt { get; init; }
}
