using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Entities;
using ContentBlogs.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Blog.TrackBlogView;

public sealed record TrackBlogViewCommand(
    Guid BlogId,
    BlogViewerKind ViewerKind,
    string ViewerId)
    : ICommand<BlogViewCountResult>;
