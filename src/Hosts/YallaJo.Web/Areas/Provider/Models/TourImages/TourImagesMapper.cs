using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Provider.Models.TourImages;

public static class TourImagesMapper
{
    public static TourImagesVm ToVm(
        Guid tourId,
        IReadOnlyList<AttachmentItemResponse> images,
        IApiAssetUrlResolver assetResolver) => new()
    {
        TourId = tourId,
        Images = images
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.UploadedAt)
            .Select(i => ToRowVm(i, assetResolver))
            .ToList(),
    };

    private static TourImageRowVm ToRowVm(AttachmentItemResponse i, IApiAssetUrlResolver assetResolver) => new()
    {
        Id            = i.Id,
        // Backend stores Attachment.Url as a relative "/uploads/..." path served by the
        // API host, not the Web host. Resolve to the API origin (same pattern as public
        // tour cards) so the browser fetches the image from the correct host instead of
        // 404ing against the Web host.
        Url           = assetResolver.Resolve(i.Url) ?? i.Url,
        FileName      = i.OriginalFileName,
        UploadedAt    = i.UploadedAt,
        FileSizeLabel = FormatSize(i.FileSize),
    };

    private static string FormatSize(long? bytes)
    {
        if (bytes is null or <= 0) return string.Empty;
        string[] units = ["B", "KB", "MB", "GB"];
        double size = bytes.Value;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1) { size /= 1024; unit++; }
        return $"{size:0.#} {units[unit]}";
    }
}
