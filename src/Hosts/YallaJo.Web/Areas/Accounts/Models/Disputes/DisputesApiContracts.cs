namespace YallaJo.Web.Areas.Accounts.Models.Disputes;

/// <summary>
/// Wire contract for <c>GET /api/v1/disputes/my</c> (Finance module, <c>DisputeDto</c>).
/// Status/Resolution are serialized from backend enums as strings.
/// </summary>
public sealed class DisputeResponse
{
    public Guid Id { get; init; }
    public Guid PaymentId { get; init; }
    public Guid UserId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? Status { get; init; }
    public string? Resolution { get; init; }
    public DateTime? ResolvedAt { get; init; }
    public Guid? ResolvedByUserId { get; init; }
    public string? ResolutionNotes { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Wire contract for <c>POST /api/v1/disputes</c> (Finance <c>OpenDisputeRequest</c>).
/// The backend resolves the <c>UserId</c> from the JWT, so only the payment id and the
/// reason/description text are sent.
/// </summary>
public sealed record OpenDisputeApiRequest(Guid PaymentId, string Reason, string Description);
