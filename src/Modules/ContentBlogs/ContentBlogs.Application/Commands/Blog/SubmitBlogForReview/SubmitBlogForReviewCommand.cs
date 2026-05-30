using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Blog.SubmitBlogForReview;

public sealed record SubmitBlogForReviewCommand(Guid BlogId, byte[] RowVersion) : ICommand;
