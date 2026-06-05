namespace YallaJo.Web.Areas.Public.Helpers;

public static class PublicImagePlaceholder
{
    // Theme placeholder pool shipped in wwwroot: assets/images/gallery/01..16.jpg.
    private const int GalleryImageCount = 16;

    /// <summary>Stable per-id placeholder for a tour cover/card.</summary>
    public static string ResolveTourImage(Guid id) => ResolveGalleryImage(id);

    /// <summary>Stable per-id placeholder for a business cover/card.</summary>
    public static string ResolveBusinessImage(Guid id) => ResolveGalleryImage(id);

    /// <summary>Stable per-id placeholder for a place cover/card (CP-3a; real
    /// place images deferred to CP-4).</summary>
    public static string ResolvePlaceImage(Guid id) => ResolveGalleryImage(id);

    private static string ResolveGalleryImage(Guid id)
    {
        var index = (PositiveHash(id) % GalleryImageCount) + 1;
        return $"/assets/images/gallery/{index:00}.jpg";
    }

    private static int PositiveHash(Guid id)
    {
        var h = id.GetHashCode();
        return h == int.MinValue ? 0 : Math.Abs(h);
    }
}
