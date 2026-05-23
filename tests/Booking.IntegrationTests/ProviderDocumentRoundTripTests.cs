using System.Globalization;
using System.Reflection;
using Booking.Application.Commands.UpdateProviderDocument;
using Booking.Application.Commands.UploadProviderDocument;
using Booking.Application.Interfaces;
using Booking.Application.Queries.GetProviderDocuments;
using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Booking.Domain.Events;
using Booking.Domain.Repositories;
using Booking.Infrastructure.BackgroundServices;
using Booking.Infrastructure.BackgroundServices.Options;
using Booking.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Storage;

namespace Booking.IntegrationTests;

/// <summary>
/// EF InMemory round-trip for TASK 3 — upload → list → duplicate-conflict → reject + re-upload,
/// plus a Pass C critical-doc backfill verification for the BG sweep.
/// </summary>
public sealed class ProviderDocumentRoundTripTests
{
    [Fact]
    public async Task Upload_then_list_returns_the_document()
    {
        var options = new DbContextOptionsBuilder<BookingDbContext>()
            .UseInMemoryDatabase($"booking-provider-docs-{Guid.NewGuid()}")
            .Options;

        await using var context = new BookingDbContext(options);
        var (handler, currentUser, _, _) = BuildUploadHandler(context, out var tourGuideId, out var userId);

        var stream = new MemoryStream(PdfBytes(128));
        var result = await handler.Handle(
            new UploadProviderDocumentCommand(
                Type: DocumentType.MoTALicense,
                FileStream: stream,
                FileName: "mota.pdf",
                ContentType: "application/pdf",
                FileSize: stream.Length,
                ExpiresAt: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1))),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var listHandler = new GetProviderDocumentsQueryHandler(
            CreateProviderDocumentRepository(context),
            currentUser,
            NullLogger<GetProviderDocumentsQueryHandler>.Instance);

        var listResult = await listHandler.Handle(new GetProviderDocumentsQuery(userId), CancellationToken.None);

        listResult.IsSuccess.Should().BeTrue();
        listResult.Value.Should().ContainSingle(d =>
            d.Type == DocumentType.MoTALicense && d.FileName == "mota.pdf");
    }

    [Fact]
    public async Task Duplicate_active_doc_of_same_type_returns_conflict()
    {
        var options = new DbContextOptionsBuilder<BookingDbContext>()
            .UseInMemoryDatabase($"booking-provider-docs-{Guid.NewGuid()}")
            .Options;

        await using var context = new BookingDbContext(options);
        var (handler, _, _, _) = BuildUploadHandler(context, out _, out _);

        async Task<Result> UploadOnceAsync()
        {
            var stream = new MemoryStream(PdfBytes(64));
            var cmd = new UploadProviderDocumentCommand(
                Type: DocumentType.MoTALicense,
                FileStream: stream,
                FileName: "mota.pdf",
                ContentType: "application/pdf",
                FileSize: stream.Length,
                ExpiresAt: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)));
            var r = await handler.Handle(cmd, CancellationToken.None);
            return new Result(r.IsSuccess, r.Outcome.ToString());
        }

        (await UploadOnceAsync()).IsSuccess.Should().BeTrue();
        var second = await UploadOnceAsync();
        second.IsSuccess.Should().BeFalse();
        second.Outcome.Should().Be("Conflict");
    }

    [Fact]
    public async Task Rejected_previous_doc_allows_new_upload_of_same_type()
    {
        var options = new DbContextOptionsBuilder<BookingDbContext>()
            .UseInMemoryDatabase($"booking-provider-docs-{Guid.NewGuid()}")
            .Options;

        await using var context = new BookingDbContext(options);
        var (handler, _, _, tourGuideId) = BuildUploadHandler(context, out var _, out var _);

        // First upload — succeeds.
        var firstStream = new MemoryStream(PdfBytes(64));
        var first = await handler.Handle(
            new UploadProviderDocumentCommand(
                Type: DocumentType.MoTALicense,
                FileStream: firstStream,
                FileName: "mota.pdf",
                ContentType: "application/pdf",
                FileSize: firstStream.Length,
                ExpiresAt: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1))),
            CancellationToken.None);
        first.IsSuccess.Should().BeTrue();

        // Reject the first row directly (no admin endpoint yet — domain method is exercised here).
        var rejected = await context.ProviderDocuments.FirstAsync(d => d.Id == first.Value!.Id);
        rejected.Reject(reviewerUserId: Guid.NewGuid(), reason: "Blurry scan", nowUtc: DateTime.UtcNow);
        await context.SaveChangesAsync();

        // Second upload — should succeed because the previous row is Rejected (not "active").
        var secondStream = new MemoryStream(PdfBytes(64));
        var second = await handler.Handle(
            new UploadProviderDocumentCommand(
                Type: DocumentType.MoTALicense,
                FileStream: secondStream,
                FileName: "mota-v2.pdf",
                ContentType: "application/pdf",
                FileSize: secondStream.Length,
                ExpiresAt: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1))),
            CancellationToken.None);

        second.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DocumentExpiryCheck_pass_C_dispatches_suspension_for_historical_critical_expired_doc()
    {
        var options = new DbContextOptionsBuilder<BookingDbContext>()
            .UseInMemoryDatabase($"booking-provider-docs-pass-c-{Guid.NewGuid()}")
            .Options;

        await using var context = new BookingDbContext(options);

        // Seed a TourGuide and a critical document already in Expired status, with
        // ExpiredNotificationSentAt already stamped (predates TASK 3) and SuspensionDispatchedAt null.
        var tourGuideId = Guid.NewGuid();
        context.TourGuides.Add(CreateTourGuide(Guid.NewGuid(), tourGuideId));

        var historicalCritical = ProviderDocument.CreateForTourGuide(
            tourGuideId: tourGuideId,
            documentType: DocumentType.MoTALicense,
            documentUrl: "/uploads/historical.pdf",
            originalFileName: "historical.pdf",
            expiresAtUtc: DateTime.UtcNow.AddDays(30));
        historicalCritical.Approve(Guid.NewGuid(), DateTime.UtcNow.AddDays(-180));
        historicalCritical.MarkExpired(DateTime.UtcNow.AddDays(-7));
        historicalCritical.ClearDomainEvents();
        historicalCritical.SuspensionDispatchedAt.Should().BeNull();

        context.ProviderDocuments.Add(historicalCritical);
        await context.SaveChangesAsync();

        var repo = CreateProviderDocumentRepository(context);
        var uow = new TestBookingUnitOfWork(context);
        var scopeFactory = new SimpleScopeFactory(new Dictionary<Type, object>
        {
            [typeof(IProviderDocumentRepository)] = repo,
            [typeof(IBookingUnitOfWork)] = uow,
        });

        var service = new DocumentExpiryCheckService(
            scopeFactory,
            new InlineOptions(new DocumentExpiryCheckOptions
            {
                Enabled = true,
                TargetUtcTime = new TimeOnly(1, 0),
                InitialDelay = TimeSpan.Zero,
                BatchSize = 100,
                ExpiringSoonWindowDays = 30,
            }),
            new BookingBackgroundServiceStatusStore(),
            NullLogger<DocumentExpiryCheckService>.Instance,
            TimeProvider.System);

        var processed = await service.RunOnceAsync(CancellationToken.None);

        processed.Should().Be(1);
        var reloaded = await context.ProviderDocuments.SingleAsync(d => d.Id == historicalCritical.Id);
        reloaded.SuspensionDispatchedAt.Should().NotBeNull();
        // The in-memory tracked aggregate still carries the suspended event.
        historicalCritical.DomainEvents
            .OfType<ProviderSuspendedDocumentExpiredDomainEvent>()
            .Should().ContainSingle();

        // Idempotency: a second run finds nothing because the repo filter excludes
        // rows where SuspensionDispatchedAt has been stamped.
        var second = await service.RunOnceAsync(CancellationToken.None);
        second.Should().Be(0);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static byte[] PdfBytes(int payloadLength)
    {
        var header = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        var result = new byte[header.Length + payloadLength];
        Buffer.BlockCopy(header, 0, result, 0, header.Length);
        return result;
    }

    private static (
        UploadProviderDocumentCommandHandler Handler,
        ICurrentUser CurrentUser,
        IFileStorageService FileStorage,
        Guid TourGuideId) BuildUploadHandler(BookingDbContext context, out Guid tourGuideId, out Guid userId)
    {
        userId = Guid.NewGuid();
        tourGuideId = Guid.NewGuid();
        var guide = CreateTourGuide(userId, tourGuideId);
        context.TourGuides.Add(guide);
        context.SaveChanges();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(userId);

        var fileStorage = Substitute.For<IFileStorageService>();
        var counter = 0;
        fileStorage.UploadAsync(
                Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                counter++;
                return new FileUploadResult($"/uploads/provider-documents/file-{counter}.pdf", $"key-{counter}", 1024);
            });

        var handler = new UploadProviderDocumentCommandHandler(
            documentRepository: CreateProviderDocumentRepository(context),
            fileStorageService: fileStorage,
            unitOfWork: new TestBookingUnitOfWork(context),
            currentUser: currentUser,
            cache: Substitute.For<HybridCache>(),
            logger: NullLogger<UploadProviderDocumentCommandHandler>.Instance);

        return (handler, currentUser, fileStorage, tourGuideId);
    }

    private static IProviderDocumentRepository CreateProviderDocumentRepository(BookingDbContext context)
    {
        var repoType = typeof(Booking.Infrastructure.DependencyInjection).Assembly
            .GetType("Booking.Infrastructure.Repositories.ProviderDocumentRepository")
            ?? throw new InvalidOperationException("ProviderDocumentRepository type not found.");

        var instance = Activator.CreateInstance(
            repoType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: [context],
            culture: CultureInfo.InvariantCulture);

        return (IProviderDocumentRepository)(instance
            ?? throw new InvalidOperationException("Failed to instantiate ProviderDocumentRepository."));
    }

    private static TourGuide CreateTourGuide(Guid userId, Guid tourGuideId)
    {
        var guide = (TourGuide)Activator.CreateInstance(typeof(TourGuide), nonPublic: true)!;
        SetProperty(guide, nameof(TourGuide.Id), tourGuideId);
        SetProperty(guide, nameof(TourGuide.UserId), userId);
        SetProperty(guide, nameof(TourGuide.IsActive), true);
        return guide;
    }

    private static void SetProperty<TValue>(object target, string propertyName, TValue value)
    {
        var property = target.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Property '{propertyName}' not found.");

        property.SetValue(target, value);
    }

    private sealed record Result(bool IsSuccess, string Outcome);

    private sealed class TestBookingUnitOfWork(BookingDbContext context) : IBookingUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => context.SaveChangesAsync(cancellationToken);
    }

    private sealed class InlineOptions(DocumentExpiryCheckOptions value) : IOptionsMonitor<DocumentExpiryCheckOptions>
    {
        public DocumentExpiryCheckOptions CurrentValue { get; } = value;
        public DocumentExpiryCheckOptions Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<DocumentExpiryCheckOptions, string?> listener) => null;
    }

    private sealed class SimpleScopeFactory(IReadOnlyDictionary<Type, object> services)
        : IServiceScopeFactory, IServiceScope, IServiceProvider
    {
        private readonly Dictionary<Type, object> _services = new(services);

        public IServiceProvider ServiceProvider => this;
        public IServiceScope CreateScope() => this;
        public object? GetService(Type serviceType)
            => _services.TryGetValue(serviceType, out var instance) ? instance : null;
        public void Dispose() { }
    }
}
