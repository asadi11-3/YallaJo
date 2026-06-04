namespace YallaJo.Web.Areas.Accounts.Models.JoinRequests;

public sealed class MyJoinRequestsVm
{
    public IReadOnlyList<JoinRequestRowVm> Requests { get; init; } = [];

    public bool HasRequests => Requests.Count > 0;
}

public sealed class JoinRequestRowVm
{
    public Guid Id { get; init; }

    public string Status { get; init; } = "Pending";

    public int ParticipantCount { get; init; }

    public string? Message { get; init; }

    public DateTime ExpiresAt { get; init; }

    public DateTime? RespondedAt { get; init; }

    public string? ResponseMessage { get; init; }

    public Guid? ResultingBookingId { get; init; }

    public DateTime CreatedAt { get; init; }

    public bool IsPending => string.Equals(Status, "Pending", StringComparison.OrdinalIgnoreCase);

    public bool IsApproved => string.Equals(Status, "Approved", StringComparison.OrdinalIgnoreCase);
}
