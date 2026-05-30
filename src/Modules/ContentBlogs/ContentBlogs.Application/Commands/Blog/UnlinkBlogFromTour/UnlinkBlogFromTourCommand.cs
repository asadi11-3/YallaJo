using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Blog.UnlinkBlogFromTour;

public sealed record UnlinkBlogFromTourCommand(
    Guid BlogId,
    Guid TourId,
    byte[] RowVersion) : ICommand;
