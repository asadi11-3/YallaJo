namespace YallaJo.Web.Areas.Provider.Models.TourImages;

public static class TourImagesMapper
{
    public static TourImagesVm ToVm(Guid tourId, IReadOnlyList<AttachmentItemResponse> images) => new()
    {
        TourId = tourId,
        Images = images
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.UploadedAt)
            .Select(ToRowVm)
            .ToList(),
    };

    private static TourImageRowVm ToRowVm(AttachmentItemResponse i) => new()
    {
        Id            = i.Id,
        Url           = i.Url,
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
