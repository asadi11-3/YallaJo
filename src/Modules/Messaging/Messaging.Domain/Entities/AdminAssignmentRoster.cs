using YallaJo.SharedKernel.Domain.Entities;

namespace Messaging.Domain.Entities;

/// <summary>
/// Round-robin roster for support ticket auto-assignment.
/// The row-level lock pattern (UPDATE TOP(1) WITH UPDLOCK,READPAST ORDER BY LastAssignedAt)
/// ensures atomic and fair distribution across concurrent requests.
/// </summary>
public sealed class AdminAssignmentRoster : BaseEntity
{
    private AdminAssignmentRoster() { } // EF Core

    /// <summary>Admin/staff user ID eligible for ticket assignment.</summary>
    public Guid AdminUserId { get; private set; }

    /// <summary>Display name for the admin.</summary>
    public string FullName { get; private set; } = string.Empty;

    /// <summary>UTC timestamp of the last ticket assigned to this admin.</summary>
    public DateTime LastAssignedAt { get; private set; } = DateTime.MinValue;

    /// <summary>Whether the admin is on leave (excluded from rotation).</summary>
    public bool IsOnLeave { get; private set; }

    /// <summary>Whether the admin is still active in the system.</summary>
    public bool IsActive { get; private set; } = true;

    // ── Factory ───────────────────────────────────────────────────────────────

    public static AdminAssignmentRoster Create(Guid adminUserId, string fullName)
        => new() { AdminUserId = adminUserId, FullName = fullName.Trim() };

    // ── Business Methods ──────────────────────────────────────────────────────

    public void RecordAssignment(DateTime assignedAt)
    {
        LastAssignedAt = assignedAt;
    }
}
