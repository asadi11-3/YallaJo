namespace YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Attachments.ViewModels;

public sealed class AttachmentListVm
{
    public AttachmentListFilterVm Filter { get; init; } = new();
    public bool HasFilter { get; init; }
    public IReadOnlyList<AttachmentRowVm> Attachments { get; init; } = [];
    public UploadAttachmentVm Upload { get; init; } = new();
}
