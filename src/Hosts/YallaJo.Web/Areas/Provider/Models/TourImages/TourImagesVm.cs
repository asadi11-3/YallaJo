namespace YallaJo.Web.Areas.Provider.Models.TourImages;

public sealed class TourImagesVm
{
    public Guid TourId { get; init; }
    public List<TourImageRowVm> Images { get; init; } = [];

    public bool HasImages => Images.Count > 0;

    public TourImageUploadVm UploadForm { get; init; } = new();
}

public sealed class TourImageRowVm
{
    public Guid Id { get; init; }
    public string Url { get; init; } = string.Empty;
    public string? FileName { get; init; }
    public DateTime UploadedAt { get; init; }

    public string FileSizeLabel { get; init; } = string.Empty;
}
