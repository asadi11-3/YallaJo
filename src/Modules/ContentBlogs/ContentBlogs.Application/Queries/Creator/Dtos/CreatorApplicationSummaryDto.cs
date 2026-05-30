using ContentBlogs.Domain.Enums;

namespace ContentBlogs.Application.Queries.Creator.Dtos;

public sealed record CreatorApplicationSummaryDto(
    Guid Id,
    Guid ApplicantUserId,
    CreatorApplicationStatus Status,
    CreatorApplicationSource Source,
    int ReapplicationCount,
    DateTime CreatedAt);
