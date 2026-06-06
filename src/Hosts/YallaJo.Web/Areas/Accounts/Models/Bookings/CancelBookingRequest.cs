namespace YallaJo.Web.Areas.Accounts.Models.Bookings;

public sealed record CancelBookingRequest(string? Reason);

/// <summary>
/// Body for POST /api/v1/booking/{id}/dispute (FE-1A). Owner-only; the backend
/// validates Reason is 10-2000 chars and the booking is Completed within 48h.
/// </summary>
public sealed record OpenBookingDisputeRequest(string Reason);
