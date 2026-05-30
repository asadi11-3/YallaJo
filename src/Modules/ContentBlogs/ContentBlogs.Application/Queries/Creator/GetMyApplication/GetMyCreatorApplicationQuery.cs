using ContentBlogs.Application.Queries.Creator.Dtos;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Queries.Creator.GetMyApplication;

/// <summary>
/// Returns the latest creator application for the current user.
/// Not cacheable — always fresh.
/// </summary>
public sealed record GetMyCreatorApplicationQuery : IQuery<CreatorApplicationDto?>;
