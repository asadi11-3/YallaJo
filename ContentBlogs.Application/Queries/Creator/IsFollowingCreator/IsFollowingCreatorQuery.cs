using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Queries.Creator.IsFollowingCreator;

/// <summary>
/// Checks if the current user is following a creator. Not cacheable.
/// </summary>
public sealed record IsFollowingCreatorQuery(Guid CreatorProfileId) : IQuery<bool>;
