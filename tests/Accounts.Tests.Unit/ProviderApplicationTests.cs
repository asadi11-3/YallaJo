using Accounts.Domain.Entities;
using Accounts.Domain.Enums;
using Accounts.Domain.Events;
using FluentAssertions;

namespace Accounts.Tests.Unit;

/// <summary>
/// Domain-level tests for the <see cref="ProviderApplication"/> aggregate.
///
/// Covers:
/// - Happy-path state machine transitions (Register → Submit → Approve/Reject/RequestDocs/Suspend/Reinstate)
/// - Business rules: cooling period, max re-applications, guard clauses for invalid states
/// - Domain events raised on each transition
/// - Document management (AddDocument, ReplaceDocument)
/// </summary>
public sealed class ProviderApplicationTests
{
    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static readonly Guid UserId  = Guid.NewGuid();
    private static readonly Guid AdminId = Guid.NewGuid();

    private static ProviderApplication CreateDraft() =>
        ProviderApplication.Register(
            userId:              UserId,
            type:                ProviderType.TourOperator,
            businessName:        "Acme Tours",
            contactEmail:        "contact@acme.com",
            contactPhone:        "+1234567890",
            address:             "123 Main St, Cairo",
            description:         "Premium tour operator in Egypt",
            typeSpecificDataJson: null).Value;

    /// <summary>Adds all required documents for a TourOperator application so Submit() succeeds.</summary>
    private static void AddRequiredDocuments(ProviderApplication app)
    {
        // TourOperator requires: BusinessLicense, TaxRegistration, TourismAuthorityLicense, InsuranceCertificate
        foreach (var docType in new[]
        {
            DocumentType.BusinessLicense,
            DocumentType.TaxRegistration,
            DocumentType.TourismAuthorityLicense,
            DocumentType.InsuranceCertificate
        })
        {
            app.AddDocument(docType, $"https://storage.example.com/docs/{docType}.pdf",
                $"{docType}.pdf", 1024 * 512, expiresAt: DateTime.UtcNow.AddYears(1));
        }
    }

    private static ProviderApplication CreatePending()
    {
        var app = CreateDraft();
        AddRequiredDocuments(app);
        app.Submit().IsSuccess.Should().BeTrue();
        return app;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Register
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Register_WithValidData_ReturnsDraftApplication()
    {
        var result = ProviderApplication.Register(
            userId:       UserId,
            type:         ProviderType.IndependentGuide,
            businessName: "Best Guide",
            contactEmail: "guide@example.com",
            contactPhone: "+9876543210",
            address:      "12 Guide Street",
            description:  "Expert city guide");

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(ProviderApplicationStatus.Draft);
        result.Value.UserId.Should().Be(UserId);
        result.Value.Type.Should().Be(ProviderType.IndependentGuide);
        result.Value.BusinessName.Should().Be("Best Guide");
    }

    [Fact]
    public void Register_WithEmptyUserId_ReturnsFailure()
    {
        var result = ProviderApplication.Register(
            userId:       Guid.Empty,
            type:         ProviderType.TourOperator,
            businessName: "Tour Co",
            contactEmail: "a@b.com",
            contactPhone: "+1",
            address:      "Addr",
            description:  "Desc");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Register_RaisesProviderRegisteredDomainEvent()
    {
        var app = CreateDraft();

        app.DomainEvents.Should().ContainSingle(e => e is ProviderRegisteredDomainEvent);
        var evt = (ProviderRegisteredDomainEvent)app.DomainEvents.Single(e => e is ProviderRegisteredDomainEvent);
        evt.ApplicationId.Should().Be(app.Id);
        evt.UserId.Should().Be(UserId);
        evt.Type.Should().Be(ProviderType.TourOperator);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Submit
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Submit_FromDraft_TransitionsToPending()
    {
        var app = CreateDraft();
        AddRequiredDocuments(app);

        var result = app.Submit();

        result.IsSuccess.Should().BeTrue();
        app.Status.Should().Be(ProviderApplicationStatus.Pending);
        app.SubmittedAt.Should().NotBeNull();
    }

    [Fact]
    public void Submit_RaisesProviderApplicationSubmittedDomainEvent()
    {
        var app = CreateDraft();
        AddRequiredDocuments(app);
        app.Submit();

        app.DomainEvents.Should().Contain(e => e is ProviderApplicationSubmittedDomainEvent);
    }

    [Fact]
    public void Submit_FromPending_ReturnsFailure()
    {
        var app = CreatePending();

        var result = app.Submit();

        result.IsFailure.Should().BeTrue();
        app.Status.Should().Be(ProviderApplicationStatus.Pending);
    }

    [Fact]
    public void Submit_FromMoreDocsNeeded_TransitionsToPending()
    {
        var app = CreatePending();
        app.RequestMoreDocs(AdminId, [DocumentType.BusinessLicense]);

        var result = app.Submit();

        result.IsSuccess.Should().BeTrue();
        app.Status.Should().Be(ProviderApplicationStatus.Pending);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Approve
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Approve_FromPending_TransitionsToApproved()
    {
        var app = CreatePending();

        var result = app.Approve(AdminId);

        result.IsSuccess.Should().BeTrue();
        app.Status.Should().Be(ProviderApplicationStatus.Approved);
        app.ReviewedAt.Should().NotBeNull();
        app.ReviewedByUserId.Should().Be(AdminId);
    }

    [Fact]
    public void Approve_RaisesProviderApprovedDomainEvent()
    {
        var app = CreatePending();
        app.Approve(AdminId);

        app.DomainEvents.Should().Contain(e => e is ProviderApprovedDomainEvent);
        var evt = app.DomainEvents.OfType<ProviderApprovedDomainEvent>().Single();
        evt.ApplicationId.Should().Be(app.Id);
        evt.ApprovedByAdminId.Should().Be(AdminId);
    }

    [Fact]
    public void Approve_FromDraft_ReturnsFailure()
    {
        var app = CreateDraft();

        var result = app.Approve(AdminId);

        result.IsFailure.Should().BeTrue();
        app.Status.Should().Be(ProviderApplicationStatus.Draft);
    }

    [Fact]
    public void Approve_AlreadyApproved_ReturnsFailure()
    {
        var app = CreatePending();
        app.Approve(AdminId);

        var result = app.Approve(AdminId);

        result.IsFailure.Should().BeTrue();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Reject
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Reject_FromPending_TransitionsToRejected_WithCoolingPeriod()
    {
        var app = CreatePending();
        var before = DateTime.UtcNow;

        var result = app.Reject(AdminId, "Insufficient documentation");

        result.IsSuccess.Should().BeTrue();
        app.Status.Should().Be(ProviderApplicationStatus.Rejected);
        app.RejectionReason.Should().Be("Insufficient documentation");
        app.CoolingPeriodEndsAt.Should().NotBeNull();
        app.CoolingPeriodEndsAt!.Value.Should().BeCloseTo(before.AddDays(7), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Reject_RaisesProviderRejectedDomainEvent()
    {
        var app = CreatePending();
        app.Reject(AdminId, "No license");

        app.DomainEvents.Should().Contain(e => e is ProviderRejectedDomainEvent);
    }

    [Fact]
    public void Reject_FromDraft_ReturnsFailure()
    {
        var app = CreateDraft();

        var result = app.Reject(AdminId, "reason");

        result.IsFailure.Should().BeTrue();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // RequestMoreDocs
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void RequestMoreDocs_FromPending_TransitionsToMoreDocsNeeded()
    {
        var app = CreatePending();
        var missingTypes = new List<DocumentType> { DocumentType.BusinessLicense, DocumentType.TaxRegistration };

        var result = app.RequestMoreDocs(AdminId, missingTypes);

        result.IsSuccess.Should().BeTrue();
        app.Status.Should().Be(ProviderApplicationStatus.MoreDocsNeeded);
        app.ReviewedByUserId.Should().Be(AdminId);
    }

    [Fact]
    public void RequestMoreDocs_RaisesProviderMoreDocsRequestedDomainEvent()
    {
        var app = CreatePending();
        app.RequestMoreDocs(AdminId, [DocumentType.GovernmentId]);

        app.DomainEvents.Should().Contain(e => e is ProviderMoreDocsRequestedDomainEvent);
    }

    [Fact]
    public void RequestMoreDocs_FromDraft_ReturnsFailure()
    {
        var app = CreateDraft();

        var result = app.RequestMoreDocs(AdminId, [DocumentType.BusinessLicense]);

        result.IsFailure.Should().BeTrue();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Suspend / Reinstate
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Suspend_FromApproved_TransitionsToSuspended()
    {
        var app = CreatePending();
        app.Approve(AdminId);

        var result = app.Suspend(AdminId, "Document expired");

        result.IsSuccess.Should().BeTrue();
        app.Status.Should().Be(ProviderApplicationStatus.Suspended);
        app.SuspensionReason.Should().Be("Document expired");
    }

    [Fact]
    public void Suspend_RaisesProviderSuspendedDomainEvent()
    {
        var app = CreatePending();
        app.Approve(AdminId);
        app.Suspend(AdminId, "Expired cert");

        app.DomainEvents.Should().Contain(e => e is ProviderSuspendedDomainEvent);
    }

    [Fact]
    public void Suspend_FromPending_ReturnsFailure()
    {
        var app = CreatePending();

        var result = app.Suspend(AdminId, "reason");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Reinstate_FromSuspended_TransitionsToApproved()
    {
        var app = CreatePending();
        app.Approve(AdminId);
        app.Suspend(AdminId, "Expired");

        var result = app.Reinstate(AdminId);

        result.IsSuccess.Should().BeTrue();
        app.Status.Should().Be(ProviderApplicationStatus.Approved);
        app.SuspensionReason.Should().BeNull();
    }

    [Fact]
    public void Reinstate_RaisesProviderReinstatedDomainEvent()
    {
        var app = CreatePending();
        app.Approve(AdminId);
        app.Suspend(AdminId, "Cert");
        app.Reinstate(AdminId);

        app.DomainEvents.Should().Contain(e => e is ProviderReinstatedDomainEvent);
    }

    [Fact]
    public void Reinstate_FromApproved_ReturnsFailure()
    {
        var app = CreatePending();
        app.Approve(AdminId);

        var result = app.Reinstate(AdminId);

        result.IsFailure.Should().BeTrue();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Re-application limit (max 3)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Submit_AfterThreeRejections_ReturnsFailure()
    {
        var app = CreateDraft();

        // First application cycle
        app.Submit();
        app.Reject(AdminId, "r1");
        // Simulate cooling period ended by backdating
        SetCoolingPeriodEnded(app);
        app.Submit(); // reapplication 1

        app.Reject(AdminId, "r2");
        SetCoolingPeriodEnded(app);
        app.Submit(); // reapplication 2

        app.Reject(AdminId, "r3");
        SetCoolingPeriodEnded(app);
        var result = app.Submit(); // 4th attempt — should fail (max 3 re-apps)

        result.IsFailure.Should().BeTrue("4th reapplication exceeds the max 3 limit");
    }

    /// <summary>
    /// Backdates CoolingPeriodEndsAt via reflection so we can fast-forward past the 7-day wait.
    /// </summary>
    private static void SetCoolingPeriodEnded(ProviderApplication app)
    {
        var prop = typeof(ProviderApplication).GetProperty(
            "CoolingPeriodEndsAt",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)!;

        // Use backing field setter via reflection to bypass private-set
        var backing = typeof(ProviderApplication).GetField(
            "<CoolingPeriodEndsAt>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

        backing.SetValue(app, DateTime.UtcNow.AddDays(-1));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Cooling period enforcement
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Submit_DuringCoolingPeriod_ReturnsFailure()
    {
        var app = CreateDraft();
        app.Submit();
        app.Reject(AdminId, "reason");

        // CoolingPeriodEndsAt is 7 days in the future — resubmit immediately should fail
        var result = app.Submit();

        result.IsFailure.Should().BeTrue("reapplication is blocked during the 7-day cooling period");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Document management
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AddDocument_AddsDocumentToCollection()
    {
        var app = CreateDraft();

        var result = app.AddDocument(
            documentType: DocumentType.BusinessLicense,
            fileUrl:       "https://storage/business-license.pdf",
            fileName:      "business-license.pdf",
            fileSizeBytes: 1024 * 500,
            expiresAt:     DateTime.UtcNow.AddYears(1));

        result.IsSuccess.Should().BeTrue();
        app.Documents.Should().HaveCount(1);
        app.Documents.Single().DocumentType.Should().Be(DocumentType.BusinessLicense);
    }

    [Fact]
    public void AddDocument_SameType_Twice_ReturnsFailure()
    {
        var app = CreateDraft();
        app.AddDocument(DocumentType.BusinessLicense, "https://s/1.pdf", "1.pdf", 1024, null);

        var result = app.AddDocument(DocumentType.BusinessLicense, "https://s/2.pdf", "2.pdf", 1024, null);

        result.IsFailure.Should().BeTrue("duplicate document type is not allowed; use ReplaceDocument instead");
    }

    [Fact]
    public void AddDocument_ExceedsMaxCount_ReturnsFailure()
    {
        var app = CreateDraft();

        // Add 10 documents (using distinct types)
        var types = Enum.GetValues<DocumentType>().Take(10).ToArray();
        foreach (var type in types)
        {
            app.AddDocument(type, $"https://s/{type}.pdf", $"{type}.pdf", 1024, null).IsSuccess.Should().BeTrue();
        }

        // 11th document should fail
        var result = app.AddDocument(DocumentType.AffiliatedGuidesList, "https://s/extra.pdf", "extra.pdf", 1024, null);
        result.IsFailure.Should().BeTrue("max 10 documents per application");
    }

    [Fact]
    public void ReplaceDocument_UpdatesExistingDocument()
    {
        var app = CreateDraft();
        app.AddDocument(DocumentType.BusinessLicense, "https://s/old.pdf", "old.pdf", 1024, null);
        var docId = app.Documents.Single().Id;

        var result = app.ReplaceDocument(docId, "https://s/new.pdf", "new.pdf", 2048, null);

        result.IsSuccess.Should().BeTrue();
        app.Documents.Single().FileUrl.Should().Be("https://s/new.pdf");
    }

    [Fact]
    public void ReplaceDocument_NonExistentId_ReturnsFailure()
    {
        var app = CreateDraft();

        var result = app.ReplaceDocument(Guid.NewGuid(), "https://s/new.pdf", "new.pdf", 1024, null);

        result.IsFailure.Should().BeTrue();
    }
}
