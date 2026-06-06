using Finance.Domain.Enums;
using Finance.Domain.Events;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.Entities;

namespace Finance.Domain.Entities;

public sealed class Dispute : AuditableEntity, IAggregateRoot
{
    private readonly List<DisputeMessage> _messages = [];
    private readonly List<DisputeEvidence> _evidence = [];

    private Dispute() { } // EF Core

    public Guid PaymentId { get; private set; }
    public Guid UserId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DisputeStatus Status { get; private set; } = DisputeStatus.Open;
    public DisputeResolution? Resolution { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public Guid? ResolvedByUserId { get; private set; }
    public string? ResolutionNotes { get; private set; }
    public Guid? ReviewedByAdminId { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public Guid? EscalatedByAdminId { get; private set; }
    public DateTime? EscalatedAt { get; private set; }
    public string? EscalationReason { get; private set; }

    public Payment Payment { get; private set; } = default!;
    public IReadOnlyCollection<DisputeMessage> Messages => _messages.AsReadOnly();
    public IReadOnlyCollection<DisputeEvidence> Evidence => _evidence.AsReadOnly();

    public static Result<Dispute> Open(Guid paymentId, Guid userId, string reason, string description)
    {
        if (paymentId == Guid.Empty || userId == Guid.Empty)
        {
            return Result.Failure<Dispute>(new Error("Dispute.InvalidOwner", "Payment and user are required."), Outcome.Invalid);
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure<Dispute>(new Error("Dispute.ReasonRequired", "Dispute reason is required."), Outcome.Invalid);
        }

        var dispute = new Dispute
        {
            PaymentId = paymentId,
            UserId = userId,
            Reason = reason.Trim(),
            Description = description?.Trim() ?? string.Empty,
            Status = DisputeStatus.Open,
        };

        dispute.AddDomainEvent(new DisputeOpenedDomainEvent(dispute.Id, paymentId, userId, dispute.Reason));
        return Result.Success(dispute);
    }

    public Result MarkUnderReview(Guid adminId, DateTime utcNow)
    {
        if (adminId == Guid.Empty)
        {
            return Result.Failure(new Error("Dispute.AdminRequired", "Admin id is required."), Outcome.Invalid);
        }

        if (Status is DisputeStatus.Resolved or DisputeStatus.Closed)
        {
            return Result.Failure(new Error("Dispute.InvalidState", $"Cannot review dispute in {Status} state."), Outcome.Conflict);
        }

        Status = DisputeStatus.UnderReview;
        ReviewedByAdminId = adminId;
        ReviewedAt = utcNow;
        MarkUpdated();
        return Result.Success();
    }

    public Result Resolve(Guid adminId, DisputeResolution resolution, string? notes, DateTime utcNow)
    {
        if (adminId == Guid.Empty)
        {
            return Result.Failure(new Error("Dispute.AdminRequired", "Admin id is required."), Outcome.Invalid);
        }

        if (Status is DisputeStatus.Resolved or DisputeStatus.Closed)
        {
            return Result.Failure(new Error("Dispute.InvalidState", $"Cannot resolve dispute in {Status} state."), Outcome.Conflict);
        }

        Status = DisputeStatus.Resolved;
        Resolution = resolution;
        ResolutionNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        ResolvedByUserId = adminId;
        ResolvedAt = utcNow;
        MarkUpdated();
        AddDomainEvent(new DisputeResolvedDomainEvent(Id, PaymentId, UserId, resolution, adminId, ResolutionNotes));
        return Result.Success();
    }

    public Result Escalate(Guid adminId, string reason, DateTime utcNow)
    {
        if (adminId == Guid.Empty)
        {
            return Result.Failure(new Error("Dispute.AdminRequired", "Admin id is required."), Outcome.Invalid);
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(new Error("Dispute.EscalationReasonRequired", "Escalation reason is required."), Outcome.Invalid);
        }

        if (Status is DisputeStatus.Resolved or DisputeStatus.Closed)
        {
            return Result.Failure(new Error("Dispute.InvalidState", $"Cannot escalate dispute in {Status} state."), Outcome.Conflict);
        }

        Status = DisputeStatus.Escalated;
        EscalatedByAdminId = adminId;
        EscalatedAt = utcNow;
        EscalationReason = reason.Trim();
        MarkUpdated();
        AddDomainEvent(new DisputeEscalatedDomainEvent(Id, PaymentId, UserId, EscalationReason, adminId));
        return Result.Success();
    }
}
