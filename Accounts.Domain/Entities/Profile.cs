using Accounts.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace Accounts.Domain.Entities;


public sealed class Profile : AuditableEntity, IAggregateRoot
{
    private Profile() { } // EF Core

    /// <summary>Logical reference to Security.User.Id — not a FK.</summary>
    public Guid UserId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? DisplayName { get; private set; }
    public string? AvatarUrl { get; private set; }
    public DateOnly? DateOfBirth { get; private set; }
    public Gender? Gender { get; private set; }
    public string? Country { get; private set; }
    public string? City { get; private set; }
    public string? AddressLine { get; private set; }

    public static Profile Create(Guid userId, string firstName, string lastName)
    {
        return new Profile
        {
            UserId = userId,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim()
        };
    }

    public void UpdateProfile(
        string firstName,
        string lastName,
        DateOnly? dateOfBirth,
        Gender? gender,
        string? country,
        string? city,
        string? addressLine)
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        DateOfBirth = dateOfBirth;
        Gender = gender;
        Country = country?.Trim();
        City = city?.Trim();
        AddressLine = addressLine?.Trim();
        MarkUpdated();
    }

    public void UpdateAvatar(string avatarUrl)
    {
        AvatarUrl = avatarUrl.Trim();
        MarkUpdated();
    }

    public void DeleteAvatar()
    {
        AvatarUrl = null;
        MarkUpdated();
    }

    public void UpdateName(string firstName, string lastName)
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        MarkUpdated();
    }

    public void SetDisplayName(string? displayName)
    {
        DisplayName = displayName?.Trim();
        MarkUpdated();
    }

    public void SetAvatarUrl(string? avatarUrl)
    {
        AvatarUrl = avatarUrl?.Trim();
        MarkUpdated();
    }

    /// <summary>
    /// Phase 3D — scrubs this profile for admin account reassignment.
    /// Called by <c>IProfileReassignmentService.ResetForReassignmentAsync</c>
    /// after the Security/Auth reassignment has succeeded, so the old
    /// owner's personal data does not remain visible on the new
    /// assignee's row.
    /// <para>
    /// Behavior:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="FirstName"/> → <c>"Pending"</c> (required field must stay valid).</description></item>
    ///   <item><description><see cref="LastName"/> → <c>"Activation"</c> (required field must stay valid).</description></item>
    ///   <item><description><see cref="DisplayName"/> → trimmed <paramref name="newEmailLocalPart"/>, or <c>null</c> if empty/whitespace.</description></item>
    ///   <item><description><see cref="AvatarUrl"/>, <see cref="DateOfBirth"/>, <see cref="Gender"/>, <see cref="Country"/>, <see cref="City"/>, <see cref="AddressLine"/> → <c>null</c>.</description></item>
    ///   <item><description><see cref="UserId"/> unchanged — the system-required link to Security.User must remain stable.</description></item>
    ///   <item><description>The row is NOT soft-deleted — Phase 3D preserves profile existence.</description></item>
    ///   <item><description><c>MarkUpdated()</c> bumps <c>UpdatedAt</c>.</description></item>
    /// </list>
    /// <para>
    /// The literal placeholder values (<c>"Pending"</c>/<c>"Activation"</c>)
    /// mirror the Security <c>AccountLifecycleState.PendingActivation</c>
    /// phrasing the reassigned account enters. No domain event is raised
    /// — Phase 3D does not add an audit timeline (deferred).
    /// </para>
    /// </summary>
    public void ResetForReassignment(string? newEmailLocalPart)
    {
        FirstName = "Pending";
        LastName  = "Activation";

        DisplayName = string.IsNullOrWhiteSpace(newEmailLocalPart)
            ? null
            : newEmailLocalPart.Trim();

        AvatarUrl   = null;
        DateOfBirth = null;
        Gender      = null;
        Country     = null;
        City        = null;
        AddressLine = null;

        MarkUpdated();
    }
}
