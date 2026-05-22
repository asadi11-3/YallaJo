using ContentBlogs.Application.Queries.Creator.Dtos;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Queries.Creator.GetMyProfile;

/// <summary>
/// Returns the creator profile for the current user.
/// Not cacheable — always fresh for the owner.
/// </summary>
public sealed record GetMyCreatorProfileQuery : IQuery<CreatorProfileDto?>;
