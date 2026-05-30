using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.BlogComment.DeleteBlogComment;

public sealed record DeleteBlogCommentCommand(
    Guid CommentId,
    byte[] RowVersion) : ICommand;
