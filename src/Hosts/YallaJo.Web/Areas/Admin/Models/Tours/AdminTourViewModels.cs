namespace YallaJo.Web.Areas.Admin.Models.Tours;

public sealed class AdminToursIndexVm
{
    public string? Status { get; init; }

    public IReadOnlyList<AdminTourRowVm> Items { get; init; } = [];

    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }

    public bool HasItems => Items.Count > 0;

    public static readonly IReadOnlyList<string> StatusFilters =
        ["Draft", "Pending", "Approved", "Rejected", "Suspended", "Archived"];
}

public sealed class AdminTourRowVm
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string StatusBadgeClass { get; init; } = "bg-secondary";
    public decimal BasePrice { get; init; }
    public string Currency { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? SubmittedAt { get; init; }
    public Guid CreatedByUserId { get; init; }
}

public sealed class AdminTourDetailsVm
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
    public string StatusBadgeClass { get; init; } = "bg-secondary";
    public bool IsInstantBooking { get; init; }
    public int CancellationPolicyHours { get; init; }
    public bool IsChildFriendly { get; init; }
    public bool IsAccessible { get; init; }
    public int? AgeRestriction { get; init; }
    public Guid? PlaceId { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    public string? PlaceName { get; set; }
    public string? PlaceCity { get; set; }
    public string? PlaceCountry { get; set; }
    public bool PlaceLookupFailed { get; set; }

    public string PlaceDisplay =>
        PlaceId is not { } id
            ? Infrastructure.Api.PlaceDisplayFormatter.NotSpecified
            : PlaceLookupFailed || string.IsNullOrWhiteSpace(PlaceName)
                ? Infrastructure.Api.PlaceDisplayFormatter.UnresolvedLabel(id)
                : Infrastructure.Api.PlaceDisplayFormatter.Format(PlaceName, PlaceCity, PlaceCountry);

    public bool HasRowVersion { get; init; }

    public bool CanApprove   => HasRowVersion && Status == "Pending";
    public bool CanReject    => HasRowVersion && Status == "Pending";
    public bool CanSuspend   => HasRowVersion && Status == "Approved";
    public bool CanReinstate => HasRowVersion && Status == "Suspended";

    public bool HasAnyAction => CanApprove || CanReject || CanSuspend || CanReinstate;
}
