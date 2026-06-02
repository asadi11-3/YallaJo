using YallaJo.Web.Areas.Admin.Models.Attachments;
using YallaJo.Web.Areas.Admin.Models.Attachments;

namespace YallaJo.Web.Areas.Admin.Models.Attachments;

public static class AttachmentsMapper
{
    public static AttachmentRowVm ToRowVm(AttachmentItemResponse r) => new()
    {
        Id               = r.Id,
        EntityId         = r.EntityId,
        EntityType       = r.EntityType,
        Type             = r.Type,
        Url              = r.Url,
        ThumbnailUrl     = r.ThumbnailUrl,
        OriginalFileName = r.OriginalFileName,
        SortOrder        = r.SortOrder,
        UploadedAt       = r.UploadedAt,
    };

    public static IReadOnlyDictionary<string, string> BuildUploadFields(UploadAttachmentVm vm)
    {
        var fields = new Dictionary<string, string>
        {
            ["EntityType"]     = vm.EntityType.ToString(),
            ["EntityId"]       = vm.EntityId.ToString(),
            ["AttachmentType"] = vm.AttachmentType.ToString(),
            ["SortOrder"]      = vm.SortOrder.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };
        if (vm.Width.HasValue)           fields["Width"]           = vm.Width.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (vm.Height.HasValue)          fields["Height"]          = vm.Height.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (vm.DurationSeconds.HasValue) fields["DurationSeconds"] = vm.DurationSeconds.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return fields;
    }
}
