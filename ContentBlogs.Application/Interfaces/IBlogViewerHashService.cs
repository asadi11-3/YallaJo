using ContentBlogs.Domain.Entities;
using ContentBlogs.Domain.Enums;

namespace ContentBlogs.Application.Interfaces;

public interface IBlogViewerHashService
{
    byte[] Hash(BlogViewerKind kind, string viewerId);
}
