namespace ContentBlogs.Presentation.Endpoints.Blog.Models;

public sealed record FeatureBlogRequest(byte[] RowVersion, DateTime? FeaturedUntil);
