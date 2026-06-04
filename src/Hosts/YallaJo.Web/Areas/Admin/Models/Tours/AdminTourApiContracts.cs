namespace YallaJo.Web.Areas.Admin.Models.Tours;

public sealed class AdminToursPaginatedResponse<T>
{
    public List<T> Items { get; init; } = [];
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
}

public sealed class AdminTourSummaryResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public decimal BasePrice { get; init; }
    public string Currency { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? SubmittedAt { get; init; }
    public Guid CreatedByUserId { get; init; }
}

public sealed class AdminTourDetailResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ShortDescription { get; init; }
    public string Difficulty { get; init; } = string.Empty;
    public int DurationMinutes { get; init; }
    public int MaxGroupSize { get; init; }
    public int? MinAge { get; init; }
    public decimal BasePrice { get; init; }
    public string Currency { get; init; } = string.Empty;
    public decimal Latitude { get; init; }
    public decimal Longitude { get; init; }
    public decimal? MeetingPointLatitude { get; init; }
    public decimal? MeetingPointLongitude { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool IsInstantBooking { get; init; }
    public int CancellationPolicyHours { get; init; }
    public bool IsChildFriendly { get; init; }
    public bool IsAccessible { get; init; }
    public int? AgeRestriction { get; init; }
    public Guid? PlaceId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    public byte[]? RowVersion { get; init; }
}


public sealed record TourRowVersionApiRequest(byte[] RowVersion);

public sealed record RejectTourApiRequest(byte[] RowVersion, string Reason);

public sealed record SuspendTourApiRequest(byte[] RowVersion, string Reason);
