using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Creator.Posts.FeaturePost;

public sealed record FeatureCreatorPostCommand(Guid PostId, DateTime? FeaturedUntil) : ICommand;
