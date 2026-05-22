using Accounts.Domain.Enums;
using Accounts.Domain.Errors;
using Accounts.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Domain.Entities;

public sealed class ProviderApplication : AuditableEntity, IAggregateRoot
{
    private ProviderApplication() { } // EF Core

    // ── Required docs per provider type ──────────────────────────────────────
    private static readonly IReadOnlyDictionary<ProviderType, IReadOnlyList<DocumentType>> RequiredDocuments =
        new Dictionary<ProviderType, IReadOnlyList<DocumentType>>
        {
            [ProviderType.TourOperator]     = [DocumentType.BusinessLicense, DocumentType.TaxRegistration, DocumentType.TourismAuthorityLicense, DocumentType.InsuranceCertificate],
            [ProviderType.IndependentGuide] = [DocumentType.GovernmentId, DocumentType.MotaLicense, DocumentType.TaxIdentificationNumber, DocumentType.InsuranceCertificate],
            [ProviderType.HotelResort]      = [DocumentType.BusinessLicense, DocumentType.TaxRegistration, DocumentType.ProofOfOwnership, DocumentType.HealthAndSafety, DocumentType.FireSafety],
            [ProviderType.ActivityCenter]   = [DocumentType.BusinessLicense, DocumentType.TaxRegistration, DocumentType.RelevantCertification, DocumentType.LiabilityInsurance, DocumentType.FireSafety],
            [ProviderType.Agency]           = [DocumentType.BusinessLicense, DocumentType.TaxRegistration, DocumentType.TourismAuthorityLicense, DocumentType.AffiliatedGuidesList, DocumentType.InsuranceCertificate],
        };

    private const int MaxReapplications = 3;
    private const int CoolingPeriodDays = 7;
    private const int MaxDocuments = 10;

    // ── Fields ────────────────────────────────────────────────────────────────
    public Guid UserId { get; private set; }
    public ProviderType Type { get; private set; }
    public string BusinessName { get; private set; } = string.Empty;
    public string ContactEmail { get; private set; } = string.Empty;
    public string ContactPhone { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public ProviderApplicationStatus Status { get; private set; }
    public DateTime? SubmittedAt { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? SuspensionReason { get; private set; }
    public int ReapplicationCount { get; private set; }
    public DateTime? CoolingPeriodEndsAt { get; private set; }
    public string? TypeSpecificDataJson { get; private set; }

    private readonly List<ProviderDocument> _documents = [];
    public IReadOnlyCollection<ProviderDocument> Documents => _documents.AsReadOnly();

    // ── Factory ───────────────────────────────────────────────────────────────
    public static Result<ProviderApplication> Register(
        Guid userId,
        ProviderType type,
        string businessName,
        string contactEmail,
        string contactPhone,
        string address,
        string description,
        string? typeSpecificDataJson = null)
    {
        if (userId == Guid.Empty)
            return Result.Failure<ProviderApplication>(ProviderApplicationErrors.NotOwner);

        var app = new ProviderApplication
        {
            UserId               = userId,
            Type                 = type,
            BusinessName         = businessName.Trim(),
            ContactEmail         = contactEmail.Trim(),
            ContactPhone         = contactPhone.Trim(),
            Address              = address.Trim(),
            Description          = description.Trim(),
            Status               = ProviderApplicationStatus.Draft,
            TypeSpecificDataJson = typeSpecificDataJson,
            ReapplicationCount   = 0,
        };

        app.AddDomainEvent(new ProviderRegisteredDomainEvent(app.Id, userId, type, DateTime.UtcNow));
        return Result.Success(app);
    }

    // ── State transitions ─────────────────────────────────────────────────────
    public Result Submit()
    {
        if (Status is not (ProviderApplicationStatus.Draft or ProviderApplicationStatus.MoreDocsNeeded))
            return Result.Failure(ProviderApplicationErrors.InvalidStatus);

        if (CoolingPeriodEndsAt.HasValue && DateTime.UtcNow < CoolingPeriodEndsAt.Value)
            return Result.Failure(ProviderApplicationErrors.CoolingPeriodActive);

        if (!HasAllRequiredDocuments())
            return Result.Failure(ProviderApplicationErrors.MissingRequiredDocuments);

        Status      = ProviderApplicationStatus.Pending;
        SubmittedAt = DateTime.UtcNow;
        MarkUpdated();
        AddDomainEvent(new ProviderApplicationSubmittedDomainEvent(Id, UserId, SubmittedAt.Value));
        return Result.Success();
    }

    public Result Approve(Guid adminUserId)
    {
        if (Status is not ProviderApplicationStatus.Pending)
            return Result.Failure(ProviderApplicationErrors.InvalidStatus);

        Status           = ProviderApplicationStatus.Approved;
        ReviewedAt       = DateTime.UtcNow;
        ReviewedByUserId = adminUserId;
        MarkUpdated();
        AddDomainEvent(new ProviderApprovedDomainEvent(Id, UserId, Type, ReviewedAt.Value, adminUserId));
        return Result.Success();
    }

    public Result Reject(Guid adminUserId, string reason)
    {
        if (Status is not ProviderApplicationStatus.Pending)
            return Result.Failure(ProviderApplicationErrors.InvalidStatus);

        if (ReapplicationCount >= MaxReapplications)
            return Result.Failure(ProviderApplicationErrors.MaxReapplicationsReached);

        Status               = ProviderApplicationStatus.Rejected;
        ReviewedAt           = DateTime.UtcNow;
        ReviewedByUserId     = adminUserId;
        RejectionReason      = reason.Trim();
        CoolingPeriodEndsAt  = DateTime.UtcNow.AddDays(CoolingPeriodDays);
        ReapplicationCount++;
        MarkUpdated();
        AddDomainEvent(new ProviderRejectedDomainEvent(Id, UserId, reason, ReviewedAt.Value));
        return Result.Success();
    }

    public Result RequestMoreDocs(Guid adminUserId, IReadOnlyList<DocumentType> missingTypes)
    {
        if (Status is not ProviderApplicationStatus.Pending)
            return Result.Failure(ProviderApplicationErrors.InvalidStatus);

        Status           = ProviderApplicationStatus.MoreDocsNeeded;
        ReviewedAt       = DateTime.UtcNow;
        ReviewedByUserId = adminUserId;
        MarkUpdated();
        AddDomainEvent(new ProviderMoreDocsRequestedDomainEvent(Id, UserId, missingTypes, ReviewedAt.Value));
        return Result.Success();
    }

    public Result Suspend(Guid adminUserId, string reason)
    {
        if (Status is not ProviderApplicationStatus.Approved)
            return Result.Failure(ProviderApplicationErrors.InvalidStatus);

        Status           = ProviderApplicationStatus.Suspended;
        ReviewedAt       = DateTime.UtcNow;
        ReviewedByUserId = adminUserId;
        SuspensionReason = reason.Trim();
        MarkUpdated();
        AddDomainEvent(new ProviderSuspendedDomainEvent(Id, UserId, reason, ReviewedAt.Value));
        return Result.Success();
    }

    public Result Reinstate(Guid adminUserId)
    {
        if (Status is not ProviderApplicationStatus.Suspended)
            return Result.Failure(ProviderApplicationErrors.InvalidStatus);

        Status           = ProviderApplicationStatus.Approved;
        ReviewedAt       = DateTime.UtcNow;
        ReviewedByUserId = adminUserId;
        SuspensionReason = null;
        MarkUpdated();
        AddDomainEvent(new ProviderReinstatedDomainEvent(Id, UserId, ReviewedAt.Value));
        return Result.Success();
    }

    // ── Document management ───────────────────────────────────────────────────
    public Result<ProviderDocument> AddDocument(
        DocumentType documentType,
        string fileUrl,
        string fileName,
        long fileSizeBytes,
        DateTime? expiresAt = null)
    {
        if (_documents.Count >= MaxDocuments)
            return Result.Failure<ProviderDocument>(ProviderApplicationErrors.TooManyDocuments);

        if (_documents.Any(d => d.DocumentType == documentType))
            return Result.Failure<ProviderDocument>(ProviderApplicationErrors.DuplicateDocumentType);

        var doc = ProviderDocument.Create(Id, documentType, fileUrl, fileName, fileSizeBytes, expiresAt);
        _documents.Add(doc);
        MarkUpdated();
        return Result.Success(doc);
    }

    public Result ReplaceDocument(
        Guid documentId,
        string fileUrl,
        string fileName,
        long fileSizeBytes,
        DateTime? expiresAt = null)
    {
        var doc = _documents.FirstOrDefault(d => d.Id == documentId);
        if (doc is null)
            return Result.Failure(ProviderApplicationErrors.DocumentNotFound);

        doc.Replace(fileUrl, fileName, fileSizeBytes, expiresAt);
        MarkUpdated();
        return Result.Success();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private bool HasAllRequiredDocuments()
    {
        if (!RequiredDocuments.TryGetValue(Type, out var required))
            return true;

        var uploaded = _documents.Select(d => d.DocumentType).ToHashSet();
        return required.All(r => uploaded.Contains(r));
    }

    public IReadOnlyList<DocumentType> GetMissingDocumentTypes()
    {
        if (!RequiredDocuments.TryGetValue(Type, out var required))
            return [];

        var uploaded = _documents.Select(d => d.DocumentType).ToHashSet();
        return required.Where(r => !uploaded.Contains(r)).ToList();
    }
}
