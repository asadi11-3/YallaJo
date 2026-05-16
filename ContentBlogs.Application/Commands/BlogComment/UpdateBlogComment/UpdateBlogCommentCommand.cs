using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.BlogComment.UpdateBlogComment;

public sealed record UpdateBlogCommentCommand(
    Guid CommentId,
    string Content) : ICommand;
