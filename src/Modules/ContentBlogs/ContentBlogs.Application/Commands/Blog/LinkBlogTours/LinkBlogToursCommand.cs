using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Blog.LinkBlogTours;

public sealed record LinkBlogToursCommand(
    Guid BlogId,
    byte[] RowVersion,
    IReadOnlyCollection<LinkBlogTourItem> Tours) : ICommand;
