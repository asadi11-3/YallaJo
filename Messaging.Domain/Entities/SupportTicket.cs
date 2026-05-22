using Messaging.Domain.Enums;
using Messaging.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;

namespace Messaging.Domain.Entities;

public sealed class SupportTicket : AuditableEntity, IAggregateRoot
{
    private readonly List<TicketMessage> _ticketMessages = [];

    private SupportTicket() { } // EF Core

    /// <summary>User who opened the ticket.</summary>
    public Guid CreatedByUserId { get; private set; }

    /// <summary>Ticket subject line (10-200 chars).</summary>
    public string Subject { get; private set; } = string.Empty;

    /// <summary>Initial problem description.</summary>
    public string InitialBody { get; private set; } = string.Empty;

    /// <summary>Category used for SLA and routing.</summary>
    public TicketCategory Category { get; private set; }

    /// <summary>Priority derived from category at creation time.</summary>
    public TicketPriority Priority { get; private set; } = TicketPriority.Medium;

    /// <summary>Lifecycle status.</summary>
    public TicketStatus Status { get; private set; } = TicketStatus.Open;

    /// <summary>Staff member currently assigned (null = unassigned).</summary>
    public Guid? AssignedToUserId { get; private set; }

    /// <summary>When the ticket was assigned.</summary>
    public DateTime? AssignedAt { get; private set; }

    /// <summary>Staff member who resolved the ticket.</summary>
    public Guid? ResolvedByUserId { get; private set; }

    /// <summary>When the ticket was resolved.</summary>
    public DateTime? ResolvedAt { get; private set; }

    /// <summary>Resolution notes recorded by staff.</summary>
    public string? ResolutionNotes { get; private set; }

    /// <summary>When the ticket was closed.</summary>
    public DateTime? ClosedAt { get; private set; }

    /// <summary>SLA deadline derived from category and creation time.</summary>
    public DateTime SlaBreachAt { get; private set; }

    public IReadOnlyCollection<TicketMessage> TicketMessages => _ticketMessages.AsReadOnly();

    // ── SLA helper ────────────────────────────────────────────────────────────

    private static TicketPriority DerivePriority(TicketCategory category) => category switch
    {
        TicketCategory.PaymentProblem    => TicketPriority.High,
        TicketCategory.BookingIssue      => TicketPriority.Medium,
        TicketCategory.ProviderComplaint => TicketPriority.Medium,
        _                                => TicketPriority.Low,
    };

    private static TimeSpan SlaDuration(TicketPriority priority) => priority switch
    {
        TicketPriority.High   => TimeSpan.FromHours(4),
        TicketPriority.Medium => TimeSpan.FromHours(12),
        _                     => TimeSpan.FromHours(24),
    };

    // ── Factory ───────────────────────────────────────────────────────────────

    public static SupportTicket Create(
        Guid createdByUserId,
        TicketCategory category,
        string subject,
        string initialBody,
        TimeProvider timeProvider)
    {
        if (createdByUserId == Guid.Empty) throw new ArgumentException("User required.", nameof(createdByUserId));
        if (string.IsNullOrWhiteSpace(subject)) throw new ArgumentException("Subject required.", nameof(subject));
        if (string.IsNullOrWhiteSpace(initialBody)) throw new ArgumentException("Body required.", nameof(initialBody));

        var priority = DerivePriority(category);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var slaBreachAt = now.Add(SlaDuration(priority));

        var ticket = new SupportTicket
        {
            CreatedByUserId = createdByUserId,
            Subject         = subject.Trim(),
            InitialBody     = initialBody.Trim(),
            Category        = category,
            Priority        = priority,
            Status          = TicketStatus.Open,
            SlaBreachAt     = slaBreachAt,
        };

        ticket.AddDomainEvent(new TicketCreatedDomainEvent(
            ticket.Id, createdByUserId, category, priority, subject, slaBreachAt));

        return ticket;
    }

    // ── Business Methods ──────────────────────────────────────────────────────

    /// <summary>Assigns the ticket to a staff member (round-robin from domain service).</summary>
    public void AssignTo(Guid adminUserId, Guid assignedByUserId, DateTime assignedAt)
    {
        if (Status == TicketStatus.Closed || Status == TicketStatus.Resolved)
            throw new InvalidOperationException($"Ticket.AlreadyClosed");

        AssignedToUserId = adminUserId;
        AssignedAt       = assignedAt;
        Status           = TicketStatus.Assigned;

        AddDomainEvent(new SupportTicketAssignedDomainEvent(Id, adminUserId, assignedByUserId, assignedAt));
        MarkUpdated();
    }

    /// <summary>Adds a message from user or staff. Staff messages may be internal.</summary>
    public void AddMessage(Guid authorUserId, string body, bool isInternal)
    {
        if (string.IsNullOrWhiteSpace(body)) throw new ArgumentException("Body required.", nameof(body));

        var msg = new TicketMessage(Id, authorUserId, body.Trim(), isInternal);
        _ticketMessages.Add(msg);

        if (Status == TicketStatus.AwaitingUser && authorUserId == CreatedByUserId)
            Status = TicketStatus.InProgress;

        AddDomainEvent(new TicketMessagePostedDomainEvent(Id, authorUserId, isInternal));
        MarkUpdated();
    }

    /// <summary>Marks the ticket as resolved by staff.</summary>
    public void Resolve(Guid resolvedByUserId, string? resolutionNotes, TimeProvider timeProvider)
    {
        if (Status == TicketStatus.Closed)
            throw new InvalidOperationException("Ticket.AlreadyClosed");
        if (Status == TicketStatus.Resolved)
            throw new InvalidOperationException("Ticket.AlreadyResolved");

        var now = timeProvider.GetUtcNow().UtcDateTime;
        ResolvedByUserId = resolvedByUserId;
        ResolvedAt       = now;
        ResolutionNotes  = resolutionNotes;
        Status           = TicketStatus.Resolved;

        AddDomainEvent(new SupportTicketResolvedDomainEvent(Id, resolvedByUserId, now, resolutionNotes));
        MarkUpdated();
    }

    /// <summary>Closes the ticket (owner or admin). Idempotent.</summary>
    public void Close(TimeProvider timeProvider)
    {
        if (Status == TicketStatus.Closed) return; // idempotent
        ClosedAt = timeProvider.GetUtcNow().UtcDateTime;
        Status   = TicketStatus.Closed;
        AddDomainEvent(new SupportTicketClosedDomainEvent(Id, CreatedByUserId));
        MarkUpdated();
    }
}
