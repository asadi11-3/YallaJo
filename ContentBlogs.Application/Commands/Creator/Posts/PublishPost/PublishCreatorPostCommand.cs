using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Creator.Posts.PublishPost;

/// <summary>
/// Allows Tier 1+ creators to publish directly, bypassing pre-review.
/// </summary>
public sealed record PublishCreatorPostCommand(Guid PostId) : ICommand;
