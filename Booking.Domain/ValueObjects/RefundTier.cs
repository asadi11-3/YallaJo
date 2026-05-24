using System.Text.Json.Serialization;

namespace Booking.Domain.ValueObjects;

public sealed record RefundTier(
    [property: JsonPropertyName("hoursBeforeTour")] int HoursBeforeTour,
    [property: JsonPropertyName("refundPercent")] decimal RefundPercent);
