namespace ContentTours.Application.Queries.Tour.Common;

public sealed record AdminTourSummaryDto(
    Guid Id,
    string Name,
    string Slug,
    string Status,
    decimal BasePrice,
    string Currency,
    DateTime CreatedAt,
    DateTime? SubmittedAt,
    Guid CreatedByUserId);
