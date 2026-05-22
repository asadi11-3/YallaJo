using ContentBlogs.Domain.Enums;
using ContentBlogs.Domain.Errors;
using ContentBlogs.Domain.Events.Creators;
using YallaJo.SharedKernel.Domain.Abstractions;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentBlogs.Domain.Entities.Creators;

/// <summary>
/// Aggregate root representing a creator invitation sent by an admin.
/// Can be Email-based (external) or InApp (existing user).
/// Expires after <see cref="ExpiryDays"/> days.
/// Wave 7 – Content Creator Module.
/// </summary>
public sealed class CreatorInvitation : AuditableEntity, IAggregateRoot
{
    public const int ExpiryDays = 14;

    // ─── Core Fields ────────────────────────────────────────────────────────

    /// <summary>Delivery channel.</summary>
    public CreatorInvitationKind Kind { get; private set; }

    /// <summary>Current status.</summary>
    public CreatorInvitationStatus Status { get; private set; }

    /// <summary>Email address for Email-kind invitations.</summary>
    public string? Email { get; private set; }

    /// <summary>User ID for InApp-kind invitations.</summary>
    public Guid? InvitedUserId { get; private set; }

    /// <summary>Unique token used for redemption.</summary>
    public string Token { get; private set; } = null!;

    /// <summary>When the invitation expires.</summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>Admin who sent the invitation.</summary>
    public Guid SentByAdminId { get; private set; }

    /// <summary>Optional personal message from the admin.</summary>
    public string? PersonalMessage { get; private set; }

    // ─── Redemption ─────────────────────────────────────────────────────────

    /// <summary>User who redeemed the invitation.</summary>
    public Guid? RedeemedByUserId { get; private set; }

    /// <summary>When the invitation was redeemed.</summary>
    public DateTime? RedeemedAt { get; private set; }

    // ─── EF Constructor ─────────────────────────────────────────────────────

    private CreatorInvitation() { }

    // ─── Factory ────────────────────────────────────────────────────────────

    /// <summary>Creates a new email-based invitation.</summary>
    public static Result<CreatorInvitation> CreateEmail(
        string email,
        Guid sentByAdminId,
        string? personalMessage)
    {
        var invitation = new CreatorInvitation
        {
            Kind = CreatorInvitationKind.Email,
            Status = CreatorInvitationStatus.Pending,
            Email = email,
            Token = GenerateToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(ExpiryDays),
            SentByAdminId = sentByAdminId,
            PersonalMessage = personalMessage
        };

        invitation.AddDomainEvent(new CreatorInvitationSentDomainEvent(
            invitation.Id, invitation.Kind, email, null, sentByAdminId));

        return Result<CreatorInvitation>.Success(invitation);
    }

    /// <summary>Creates a new in-app invitation.</summary>
    public static Result<CreatorInvitation> CreateInApp(
        Guid invitedUserId,
        Guid sentByAdminId,
        string? personalMessage)
    {
        var invitation = new CreatorInvitation
        {
            Kind = CreatorInvitationKind.InApp,
            Status = CreatorInvitationStatus.Pending,
            InvitedUserId = invitedUserId,
            Token = GenerateToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(ExpiryDays),
            SentByAdminId = sentByAdminId,
            PersonalMessage = personalMessage
        };

        invitation.AddDomainEvent(new CreatorInvitationSentDomainEvent(
            invitation.Id, invitation.Kind, null, invitedUserId, sentByAdminId));

        return Result<CreatorInvitation>.Success(invitation);
    }

    // ─── State Transitions ──────────────────────────────────────────────────

    /// <summary>Redeem the invitation by a user.</summary>
    public Result Redeem(Guid userId)
    {
        if (Status == CreatorInvitationStatus.Redeemed)
            return Result.Failure(CreatorInvitationErrors.AlreadyRedeemed);

        if (Status == CreatorInvitationStatus.Expired)
            return Result.Failure(CreatorInvitationErrors.Expired);

        if (Status == CreatorInvitationStatus.Revoked)
            return Result.Failure(CreatorInvitationErrors.Revoked);

        if (DateTime.UtcNow > ExpiresAt)
        {
            Status = CreatorInvitationStatus.Expired;
            MarkUpdated();
            return Result.Failure(CreatorInvitationErrors.Expired);
        }

        Status = CreatorInvitationStatus.Redeemed;
        RedeemedByUserId = userId;
        RedeemedAt = DateTime.UtcNow;
        MarkUpdated();
        AddDomainEvent(new CreatorInvitationRedeemedDomainEvent(Id, userId));

        return Result.Success();
    }

    /// <summary>Revoke the invitation (admin action).</summary>
    public Result Revoke()
    {
        if (Status != CreatorInvitationStatus.Pending)
            return Result.Failure(CreatorInvitationErrors.NotPending);

        Status = CreatorInvitationStatus.Revoked;
        MarkUpdated();

        return Result.Success();
    }

    /// <summary>Mark as expired (batch cleanup service).</summary>
    public Result Expire()
    {
        if (Status != CreatorInvitationStatus.Pending)
            return Result.Failure(CreatorInvitationErrors.NotPending);

        Status = CreatorInvitationStatus.Expired;
        MarkUpdated();
        AddDomainEvent(new CreatorInvitationExpiredDomainEvent(Id));

        return Result.Success();
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    private static string GenerateToken()
    {
        return Convert.ToBase64String(Guid.CreateVersion7().ToByteArray())
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}
