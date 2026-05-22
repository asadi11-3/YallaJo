using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Creator.Posts.SubmitPostForReview;

public sealed record SubmitCreatorPostForReviewCommand(Guid PostId) : ICommand;
