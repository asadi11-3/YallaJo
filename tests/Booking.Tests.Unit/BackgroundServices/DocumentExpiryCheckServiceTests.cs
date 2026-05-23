using System.Reflection;
using Booking.Application.Interfaces;
using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Booking.Domain.Events;
using Booking.Domain.Repositories;
using Booking.Infrastructure.BackgroundServices;
using Booking.Infrastructure.BackgroundServices.Options;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using YallaJo.Tests.Shared;

namespace Booking.Tests.Unit.BackgroundServices;

public sealed class DocumentExpiryCheckServiceTests
{
    [Fact]
    public async Task RunOnceAsync_marks_expiring_document_and_raises_expiring_event_once()
    {
        var (service, repo, uow, _) = BuildSubject(out _);
        var expiring = CreateApprovedDocument(expiresAtUtc: DateTime.UtcNow.AddDays(15));

        repo.GetNewlyExpiredAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProviderDocument>());
        repo.GetExpiringSoonAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProviderDocument> { expiring });

        var processed = await service.RunOnceAsync(CancellationToken.None);

        processed.Should().Be(1);
        expiring.ExpiringNotificationSentAt.Should().NotBeNull();
        expiring.DomainEvents.OfType<ProviderDocumentExpiringDomainEvent>().Should().ContainSingle();
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunOnceAsync_marks_expired_document_and_raises_expired_event_once()
    {
        var (service, repo, uow, _) = BuildSubject(out _);
        var expired = CreateApprovedDocument(expiresAtUtc: DateTime.UtcNow.AddDays(-1));

        repo.GetNewlyExpiredAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProviderDocument> { expired });
        repo.GetExpiringSoonAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProviderDocument>());

        var processed = await service.RunOnceAsync(CancellationToken.None);

        processed.Should().Be(1);
        expired.Status.Should().Be(DocumentStatus.Expired);
        expired.ExpiredNotificationSentAt.Should().NotBeNull();
        expired.DomainEvents.OfType<ProviderDocumentExpiredDomainEvent>().Should().ContainSingle();
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunOnceAsync_does_not_raise_duplicate_event_when_warning_already_sent()
    {
        var (service, repo, uow, _) = BuildSubject(out _);
        var alreadyWarned = CreateApprovedDocument(expiresAtUtc: DateTime.UtcNow.AddDays(10));
        alreadyWarned.MarkExpiring(DateTime.UtcNow);
        alreadyWarned.ClearDomainEvents();

        repo.GetNewlyExpiredAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProviderDocument>());
        repo.GetExpiringSoonAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProviderDocument> { alreadyWarned });

        var processed = await service.RunOnceAsync(CancellationToken.None);

        // Service still iterates the row, but MarkExpiring guards on ExpiringNotificationSentAt,
        // so no new event is raised and no save occurs (totalChanged = 0).
        processed.Should().Be(1); // counted because the loop touched it
        alreadyWarned.DomainEvents.OfType<ProviderDocumentExpiringDomainEvent>().Should().BeEmpty();
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunOnceAsync_with_no_documents_does_not_save()
    {
        var (service, repo, uow, _) = BuildSubject(out _);

        repo.GetNewlyExpiredAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProviderDocument>());
        repo.GetExpiringSoonAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProviderDocument>());
        repo.GetExpiredCriticalPendingSuspensionAsync(
                Arg.Any<IReadOnlyCollection<DocumentType>>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProviderDocument>());

        var processed = await service.RunOnceAsync(CancellationToken.None);

        processed.Should().Be(0);
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── TASK 3 Pass C: critical-doc suspension backfill ───────────────────────

    [Fact]
    public async Task RunOnceAsync_pass_C_emits_suspension_event_for_critical_expired_doc()
    {
        var (service, repo, uow, _) = BuildSubject(out _);
        var critical = CreateExpiredCriticalDocument(DocumentType.MoTALicense);

        // Pass A + B return nothing; only Pass C surfaces the critical row.
        repo.GetNewlyExpiredAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProviderDocument>());
        repo.GetExpiringSoonAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProviderDocument>());
        repo.GetExpiredCriticalPendingSuspensionAsync(
                Arg.Any<IReadOnlyCollection<DocumentType>>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<ProviderDocument> { critical });

        var processed = await service.RunOnceAsync(CancellationToken.None);

        processed.Should().Be(1);
        critical.SuspensionDispatchedAt.Should().NotBeNull();
        critical.DomainEvents
            .OfType<ProviderSuspendedDocumentExpiredDomainEvent>()
            .Should().ContainSingle();
        // Crucially: Pass C does NOT re-emit the standard expired event.
        critical.DomainEvents
            .OfType<ProviderDocumentExpiredDomainEvent>()
            .Should().BeEmpty();
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunOnceAsync_pass_C_is_idempotent_when_suspension_already_dispatched()
    {
        var (service, repo, uow, _) = BuildSubject(out _);

        // The repository filter would normally exclude these rows, but if the same doc somehow
        // surfaces (e.g. legacy data) the domain method must still no-op.
        var alreadyDispatched = CreateExpiredCriticalDocument(DocumentType.InsuranceCertificate);
        alreadyDispatched.MarkSuspensionDispatched(DateTime.UtcNow.AddDays(-1));
        alreadyDispatched.ClearDomainEvents();

        repo.GetNewlyExpiredAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProviderDocument>());
        repo.GetExpiringSoonAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProviderDocument>());
        repo.GetExpiredCriticalPendingSuspensionAsync(
                Arg.Any<IReadOnlyCollection<DocumentType>>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<ProviderDocument> { alreadyDispatched });

        var processed = await service.RunOnceAsync(CancellationToken.None);

        processed.Should().Be(1); // The loop touched it, but...
        alreadyDispatched.DomainEvents
            .OfType<ProviderSuspendedDocumentExpiredDomainEvent>()
            .Should().BeEmpty("MarkSuspensionDispatched is idempotent");
    }

    [Fact]
    public async Task RunOnceAsync_pass_A_does_not_emit_suspension_event_even_for_critical_doc()
    {
        var (service, repo, uow, _) = BuildSubject(out _);
        var criticalApproved = CreateApprovedCriticalDocument(DocumentType.LiabilityInsurance);

        // Pass A surfaces it (about-to-be-expired); Pass C list is empty because the row's
        // Status is still Approved when the repo query runs (BG sweep snapshots).
        repo.GetNewlyExpiredAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProviderDocument> { criticalApproved });
        repo.GetExpiringSoonAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProviderDocument>());
        repo.GetExpiredCriticalPendingSuspensionAsync(
                Arg.Any<IReadOnlyCollection<DocumentType>>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProviderDocument>());

        await service.RunOnceAsync(CancellationToken.None);

        criticalApproved.DomainEvents
            .OfType<ProviderDocumentExpiredDomainEvent>()
            .Should().ContainSingle();
        criticalApproved.DomainEvents
            .OfType<ProviderSuspendedDocumentExpiredDomainEvent>()
            .Should().BeEmpty("Pass A only emits expired event; suspension is Pass C's job (C2)");
        criticalApproved.SuspensionDispatchedAt
            .Should().BeNull("suspension is dispatched only by Pass C");
    }

    private static (
        DocumentExpiryCheckService Service,
        IProviderDocumentRepository Repo,
        IBookingUnitOfWork Uow,
        IBookingBackgroundServiceStatusStore StatusStore) BuildSubject(out FakeTimeProvider timeProvider)
    {
        var repo = Substitute.For<IProviderDocumentRepository>();
        var uow = Substitute.For<IBookingUnitOfWork>();
        IBookingBackgroundServiceStatusStore statusStore = new BookingBackgroundServiceStatusStore();
        timeProvider = new FakeTimeProvider(DateTimeOffset.Parse(
            "2026-07-15T12:00:00Z",
            System.Globalization.CultureInfo.InvariantCulture));

        var scopeFactory = new TestServiceScopeFactory(new Dictionary<Type, object>
        {
            [typeof(IProviderDocumentRepository)] = repo,
            [typeof(IBookingUnitOfWork)] = uow,
        });

        var options = new InlineOptionsMonitor<DocumentExpiryCheckOptions>(new DocumentExpiryCheckOptions
        {
            Enabled = true,
            TargetUtcTime = new TimeOnly(1, 0),
            InitialDelay = TimeSpan.Zero,
            BatchSize = 100,
            ExpiringSoonWindowDays = 30,
        });

        var service = new DocumentExpiryCheckService(
            scopeFactory,
            options,
            statusStore,
            NullLogger<DocumentExpiryCheckService>.Instance,
            timeProvider);

        return (service, repo, uow, statusStore);
    }

    private static ProviderDocument CreateApprovedDocument(DateTime expiresAtUtc)
        => CreateDocument(
            type: DocumentType.License,
            status: DocumentStatus.Approved,
            expiresAtUtc: expiresAtUtc);

    private static ProviderDocument CreateApprovedCriticalDocument(DocumentType type, DateTime? expiresAtUtc = null)
        => CreateDocument(
            type: type,
            status: DocumentStatus.Approved,
            expiresAtUtc: expiresAtUtc ?? DateTime.UtcNow.AddDays(-1));

    private static ProviderDocument CreateExpiredCriticalDocument(DocumentType type)
    {
        var doc = CreateDocument(
            type: type,
            status: DocumentStatus.Expired,
            expiresAtUtc: DateTime.UtcNow.AddDays(-7));
        SetProperty(doc, nameof(ProviderDocument.ExpiredNotificationSentAt), (DateTime?)DateTime.UtcNow.AddDays(-7));
        return doc;
    }

    private static ProviderDocument CreateDocument(
        DocumentType type,
        DocumentStatus status,
        DateTime expiresAtUtc)
    {
        var doc = (ProviderDocument)Activator.CreateInstance(
            typeof(ProviderDocument),
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            binder: null,
            args: null,
            culture: null)!;

        SetProperty(doc, nameof(ProviderDocument.Id), Guid.NewGuid());
        SetProperty(doc, nameof(ProviderDocument.TourGuideId), Guid.NewGuid());
        SetProperty(doc, nameof(ProviderDocument.DocumentType), type);
        SetProperty(doc, nameof(ProviderDocument.DocumentUrl), "/uploads/doc.pdf");
        SetProperty(doc, nameof(ProviderDocument.Status), status);
        SetProperty(doc, nameof(ProviderDocument.ExpiresAt), (DateTime?)expiresAtUtc);

        return doc;
    }

    private static void SetProperty<TValue>(object target, string propertyName, TValue value)
    {
        var property = target.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Property '{propertyName}' not found.");

        property.SetValue(target, value);
    }
}
