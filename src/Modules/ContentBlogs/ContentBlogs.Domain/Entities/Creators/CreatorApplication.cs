using ContentBlogs.Domain.Enums;
using ContentBlogs.Domain.Errors;
using ContentBlogs.Domain.Events.Creators;
using YallaJo.SharedKernel.Domain.Abstractions;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentBlogs.Domain.Entities.Creators;

/// <summary>
/// Aggregate root representing a content creator application.
/// State machine: Draft → Pending → Approved | Rejected | MoreInfoNeeded.
/// Rejected applicants may re-apply up to <see cref="MaxReapplications"/> times
/// after a <see cref="CoolingPeriodDays"/>-day cooling period.
/// Wave 7 – Content Creator Module.
/// </summary>
public sealed class CreatorApplication : AuditableEntity, IAggregateRoot
{
    public const int MaxReapplications = 3;
    public const int CoolingPeriodDays = 14;

    // ─── Core Fields ────────────────────────────────────────────────────────

    /// <summary>User who submitted the application.</summary>
    public Guid ApplicantUserId { get; private set; }

    /// <summary>Current status in the lifecycle.</summary>
    public CreatorApplicationStatus Status { get; private set; }

    /// <summary>How this application was initiated.</summary>
    public CreatorApplicationSource Source { get; private set; }

    /// <summary>Invitation Id if this application originated from an invitation.</summary>
    public Guid? InvitationId { get; private set; }

    // ─── Applicant-Provided Data (JSON columns) ─────────────────────────────

    /// <summary>Free-text bio / motivation statement.</summary>
    public string? Bio { get; private set; }

    /// <summary>Portfolio URLs (JSON array).</summary>
    public List<string> PortfolioUrls { get; private set; } = [];

    /// <summary>Sample work URLs (JSON array).</summary>
    public List<string> SampleWorkUrls { get; private set; } = [];

    /// <summary>Selected niche IDs (JSON array of Guid).</summary>
    public List<Guid> NicheIds { get; private set; } = [];

    /// <summary>Free-form tags the applicant added (JSON array).</summary>
    public List<string> FreeTags { get; private set; } = [];

    /// <summary>Language IDs the applicant can write in (JSON array of Guid).</summary>
    public List<Guid> LanguageIds { get; private set; } = [];

    /// <summary>Preferred region IDs (JSON array of Guid).</summary>
    public List<Guid> PreferredRegionIds { get; private set; } = [];

    /// <summary>Social media handles (JSON dictionary).</summary>
    public Dictionary<string, string> SocialHandles { get; private set; } = [];

    // ─── Admin Decision Fields ──────────────────────────────────────────────

    /// <summary>Admin who made the final decision.</summary>
    public Guid? ReviewedByAdminId { get; private set; }

    /// <summary>Date the decision was made.</summary>
    public DateTime? ReviewedAt { get; private set; }

    /// <summary>Admin note (rejection reason or more-info request).</summary>
    public string? AdminNote { get; private set; }

    // ─── Re-application Tracking ────────────────────────────────────────────

    /// <summary>Number of times this user has re-applied (0 = first attempt).</summary>
    public int ReapplicationCount { get; private set; }

    /// <summary>Date of the last rejection (used for cooling period calculation).</summary>
    public DateTime? LastRejectedAt { get; private set; }

    // ─── EF Constructor ─────────────────────────────────────────────────────

    private CreatorApplication() { }

    // ─── Factory ────────────────────────────────────────────────────────────

    /// <summary>Creates a new draft creator application.</summary>
    public static Result<CreatorApplication> Create(
        Guid applicantUserId,
        CreatorApplicationSource source,
        Guid? invitationId,
        string? bio,
        List<string>? portfolioUrls,
        List<string>? sampleWorkUrls,
        List<Guid>? nicheIds,
        List<string>? freeTags,
        List<Guid>? languageIds,
        List<Guid>? preferredRegionIds,
        Dictionary<string, string>? socialHandles)
    {
        var application = new CreatorApplication
        {
            ApplicantUserId = applicantUserId,
            Status = CreatorApplicationStatus.Draft,
            Source = source,
            InvitationId = invitationId,
            Bio = bio,
            PortfolioUrls = portfolioUrls ?? [],
            SampleWorkUrls = sampleWorkUrls ?? [],
            NicheIds = nicheIds ?? [],
            FreeTags = freeTags ?? [],
            LanguageIds = languageIds ?? [],
            PreferredRegionIds = preferredRegionIds ?? [],
            SocialHandles = socialHandles ?? [],
            ReapplicationCount = 0
        };

        return Result<CreatorApplication>.Success(application);
    }

    /// <summary>Creates a new draft application for a re-applying user.</summary>
    public static Result<CreatorApplication> CreateReapplication(
        Guid applicantUserId,
        int previousReapplicationCount,
        DateTime? lastRejectedAt,
        CreatorApplicationSource source,
        Guid? invitationId,
        string? bio,
        List<string>? portfolioUrls,
        List<string>? sampleWorkUrls,
        List<Guid>? nicheIds,
        List<string>? freeTags,
        List<Guid>? languageIds,
        List<Guid>? preferredRegionIds,
        Dictionary<string, string>? socialHandles)
    {
        if (previousReapplicationCount >= MaxReapplications)
            return Result<CreatorApplication>.Failure(CreatorApplicationErrors.MaxReapplicationsReached);

        if (lastRejectedAt.HasValue &&
            (DateTime.UtcNow - lastRejectedAt.Value).TotalDays < CoolingPeriodDays)
            return Result<CreatorApplication>.Failure(CreatorApplicationErrors.CoolingPeriodActive);

        var application = new CreatorApplication
        {
            ApplicantUserId = applicantUserId,
            Status = CreatorApplicationStatus.Draft,
            Source = source,
            InvitationId = invitationId,
            Bio = bio,
            PortfolioUrls = portfolioUrls ?? [],
            SampleWorkUrls = sampleWorkUrls ?? [],
            NicheIds = nicheIds ?? [],
            FreeTags = freeTags ?? [],
            LanguageIds = languageIds ?? [],
            PreferredRegionIds = preferredRegionIds ?? [],
            SocialHandles = socialHandles ?? [],
            ReapplicationCount = previousReapplicationCount + 1
        };

        return Result<CreatorApplication>.Success(application);
    }

    // ─── State Transitions ──────────────────────────────────────────────────

    /// <summary>Submit the draft application for admin review.</summary>
    public Result Submit()
    {
        if (Status != CreatorApplicationStatus.Draft)
            return Result.Failure(CreatorApplicationErrors.NotDraft);

        Status = CreatorApplicationStatus.Pending;
        MarkUpdated();
        AddDomainEvent(new CreatorApplicationSubmittedDomainEvent(Id, ApplicantUserId));

        return Result.Success();
    }

    /// <summary>Resubmit after more info was provided.</summary>
    public Result Resubmit(
        string? bio,
        List<string>? portfolioUrls,
        List<string>? sampleWorkUrls,
        List<Guid>? nicheIds,
        List<string>? freeTags,
        List<Guid>? languageIds,
        List<Guid>? preferredRegionIds,
        Dictionary<string, string>? socialHandles)
    {
        if (Status != CreatorApplicationStatus.MoreInfoNeeded)
            return Result.Failure(CreatorApplicationErrors.NotMoreInfoNeeded);

        Bio = bio ?? Bio;
        PortfolioUrls = portfolioUrls ?? PortfolioUrls;
        SampleWorkUrls = sampleWorkUrls ?? SampleWorkUrls;
        NicheIds = nicheIds ?? NicheIds;
        FreeTags = freeTags ?? FreeTags;
        LanguageIds = languageIds ?? LanguageIds;
        PreferredRegionIds = preferredRegionIds ?? PreferredRegionIds;
        SocialHandles = socialHandles ?? SocialHandles;

        Status = CreatorApplicationStatus.Pending;
        MarkUpdated();
        AddDomainEvent(new CreatorApplicationSubmittedDomainEvent(Id, ApplicantUserId));

        return Result.Success();
    }

    /// <summary>Admin approves the application.</summary>
    public Result Approve(Guid adminId)
    {
        if (Status != CreatorApplicationStatus.Pending)
            return Result.Failure(CreatorApplicationErrors.NotPending);

        Status = CreatorApplicationStatus.Approved;
        ReviewedByAdminId = adminId;
        ReviewedAt = DateTime.UtcNow;
        MarkUpdated();
        AddDomainEvent(new CreatorApplicationApprovedDomainEvent(Id, ApplicantUserId, adminId));

        return Result.Success();
    }

    /// <summary>Admin rejects the application.</summary>
    public Result Reject(Guid adminId, string reason)
    {
        if (Status != CreatorApplicationStatus.Pending)
            return Result.Failure(CreatorApplicationErrors.NotPending);

        Status = CreatorApplicationStatus.Rejected;
        ReviewedByAdminId = adminId;
        ReviewedAt = DateTime.UtcNow;
        AdminNote = reason;
        LastRejectedAt = DateTime.UtcNow;
        MarkUpdated();
        AddDomainEvent(new CreatorApplicationRejectedDomainEvent(Id, ApplicantUserId, adminId, reason));

        return Result.Success();
    }

    /// <summary>Admin requests more information.</summary>
    public Result RequestMoreInfo(Guid adminId, string note)
    {
        if (Status != CreatorApplicationStatus.Pending)
            return Result.Failure(CreatorApplicationErrors.NotPending);

        Status = CreatorApplicationStatus.MoreInfoNeeded;
        ReviewedByAdminId = adminId;
        ReviewedAt = DateTime.UtcNow;
        AdminNote = note;
        MarkUpdated();
        AddDomainEvent(new CreatorApplicationMoreInfoRequestedDomainEvent(Id, ApplicantUserId, adminId, note));

        return Result.Success();
    }
}
