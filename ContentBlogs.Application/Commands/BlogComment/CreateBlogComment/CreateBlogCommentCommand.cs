using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.BlogComment.CreateBlogComment;

public sealed record CreateBlogCommentCommand(
    Guid BlogId,
    string Content,
    Guid? ParentCommentId = null) : ICommand<CreateBlogCommentResult>;
