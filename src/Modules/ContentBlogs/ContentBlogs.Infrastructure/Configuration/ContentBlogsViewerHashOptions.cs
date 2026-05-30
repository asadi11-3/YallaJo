namespace ContentBlogs.Infrastructure.Configuration;

public sealed class ContentBlogsViewerHashOptions
{
    public const string SectionName = "ContentBlogs";

    public string ViewerHashSecret { get; set; } = string.Empty;
}
