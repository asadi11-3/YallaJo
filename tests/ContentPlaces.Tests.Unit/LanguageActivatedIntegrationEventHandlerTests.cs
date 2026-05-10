using ContentCore.Contracts.IntegrationEvents;
using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Entities;
using ContentPlaces.Infrastructure.EventHandlers;
using ContentPlaces.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Infrastructure.Inbox;

namespace ContentPlaces.Tests.Unit;

/// <summary>
/// CONTENTPLACES-FOLLOWUP-LANGACT-001 regression tests.
///
/// <para>
/// <see cref="LanguageActivatedIntegrationEventHandler"/> must:
/// </para>
///
/// <list type="bullet">
///   <item>Use projection-first anti-join queries (no <c>Include</c>-heavy graph load).</item>
///   <item>Fetch at most <c>BatchSize</c> rows per <c>ToListAsync</c> via
///   <c>OrderBy(Id).Take(BatchSize)</c>.</item>
///   <item><c>SaveChangesAsync</c> per batch.</item>
///   <item>Mark the inbox processed only after every batch succeeds, in a final
///   separate <c>SaveChangesAsync</c>.</item>
///   <item>Throw <see cref="InvalidOperationException"/> when the orchestrator
///   returns no translated set — never silently skip and never mark the inbox.</item>
/// </list>
///
/// <para>
/// Mirrors the ContentTours P1-004.1 test contract.  Uses the real
/// <see cref="ContentPlacesDbContext"/> via the EF InMemory provider so the
/// anti-join queries actually run and per-batch <c>SaveChanges</c> semantics
/// are exercised end-to-end.
/// </para>
/// </summary>
public sealed class LanguageActivatedIntegrationEventHandlerTests
{
    private const int BatchSize = 200; // mirrors handler constant

    // ── Test doubles ──────────────────────────────────────────────────────────

    /// <summary>
    /// Test UoW that delegates to the InMemory <see cref="ContentPlacesDbContext"/>
    /// (so anti-join queries see the prior batch's writes) and records a save
    /// counter so batching can be asserted.  Also supports an injectable
    /// throw-on-call hook to simulate per-batch failures.
    /// </summary>
    private sealed class CountingUnitOfWork(ContentPlacesDbContext ctx) : IContentPlacesUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Func<int, Exception?>? FailOnSave { get; set; }

        public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            SaveCount++;
            if (FailOnSave is not null)
            {
                var ex = FailOnSave(SaveCount);
                if (ex is not null) throw ex;
            }

            return await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Test inbox store that delegates to the InMemory DbContext.  Mirrors the
    /// production registration so every test exercises the real
    /// <c>InboxMessage</c> table contract.
    /// </summary>
    private sealed class TestInboxStore(ContentPlacesDbContext ctx) : IContentPlacesInboxStore
    {
        public Task<bool> HasBeenProcessedAsync(Guid messageId, CancellationToken ct = default)
            => ctx.Set<InboxMessage>().AnyAsync(m => m.Id == messageId, ct);

        public void MarkAsProcessed(Guid messageId)
            => ctx.Set<InboxMessage>().Add(InboxMessage.Create(messageId));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ContentPlacesDbContext NewInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ContentPlacesDbContext>()
            .UseInMemoryDatabase(databaseName: $"places-tests-{Guid.NewGuid():N}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ContentPlacesDbContext(options);
    }

    private static IntegrationEventNotification<LanguageActivatedIntegrationEvent> NewNotification(
        Guid? messageId = null,
        Guid? languageId = null,
        string languageCode = "fr") =>
        new(
            messageId ?? Guid.NewGuid(),
            new LanguageActivatedIntegrationEvent(
                languageId ?? Guid.NewGuid(),
                languageCode));

    private static IEntityTranslationOrchestrator BuildOrchestrator()
    {
        var orchestrator = Substitute.For<IEntityTranslationOrchestrator>();
        orchestrator
            .TranslateAsync(
                Arg.Any<IReadOnlyDictionary<string, string>>(),
                Arg.Any<string>(),
                Arg.Any<IReadOnlyList<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var fields     = call.Arg<IReadOnlyDictionary<string, string>>();
                var targets    = call.Arg<IReadOnlyList<string>>();
                var lang       = targets[0];
                var translated = fields.ToDictionary(
                    kv => kv.Key,
                    kv => $"{kv.Value}-{lang}",
                    StringComparer.Ordinal);

                IReadOnlyList<EntityFieldTranslationSet> result =
                    new[] { new EntityFieldTranslationSet(Guid.NewGuid(), lang, translated) };
                return Task.FromResult(result);
            });
        return orchestrator;
    }

    private static LanguageActivatedIntegrationEventHandler BuildHandler(
        ContentPlacesDbContext ctx,
        IEntityTranslationOrchestrator orchestrator,
        IContentPlacesUnitOfWork uow,
        IContentPlacesInboxStore inbox) =>
        new(
            ctx,
            orchestrator,
            uow,
            inbox,
            Substitute.For<ILogger<LanguageActivatedIntegrationEventHandler>>());

    private static Place SeedPlace(ContentPlacesDbContext ctx)
    {
        var place = TestPlaceFactory.CreatePlace(
            createdByUserId: Guid.NewGuid(),
            slug: $"slug-{Guid.NewGuid():N}");
        ctx.Places.Add(place);
        return place;
    }

    private static Business SeedBusiness(ContentPlacesDbContext ctx)
    {
        var business = TestBusinessFactory.CreateBusiness(ownerId: Guid.NewGuid());
        ctx.Businesses.Add(business);
        return business;
    }

    // ── 1. Inbox already processed: short-circuit ─────────────────────────────

    [Fact]
    public async Task LanguageActivated_WhenInboxAlreadyProcessed_DoesNothing()
    {
        await using var ctx = NewInMemoryContext();

        var notification = NewNotification();
        ctx.Set<InboxMessage>().Add(InboxMessage.Create(notification.MessageId));
        await ctx.SaveChangesAsync();

        // Seed entities that WOULD need translations if the handler ran.
        SeedPlace(ctx);
        SeedBusiness(ctx);
        await ctx.SaveChangesAsync();

        var orchestrator = BuildOrchestrator();
        var uow          = new CountingUnitOfWork(ctx);
        var inbox        = new TestInboxStore(ctx);
        var handler      = BuildHandler(ctx, orchestrator, uow, inbox);

        await handler.Handle(notification, CancellationToken.None);

        (await ctx.PlaceTranslations.CountAsync()).Should().Be(0);
        (await ctx.BusinessTranslations.CountAsync()).Should().Be(0);

        uow.SaveCount.Should().Be(0,
            "the inbox-already-processed branch must not invoke SaveChangesAsync");

        await orchestrator.DidNotReceiveWithAnyArgs().TranslateAsync(
            default!, default!, default!, default);
    }

    // ── 2. Adds missing PlaceTranslation rows ─────────────────────────────────

    [Fact]
    public async Task LanguageActivated_AddsMissingPlaceTranslations()
    {
        await using var ctx = NewInMemoryContext();

        var p1 = SeedPlace(ctx);
        var p2 = SeedPlace(ctx);
        await ctx.SaveChangesAsync();

        var notification = NewNotification(languageCode: "fr");
        var orchestrator = BuildOrchestrator();
        var uow          = new CountingUnitOfWork(ctx);
        var inbox        = new TestInboxStore(ctx);
        var handler      = BuildHandler(ctx, orchestrator, uow, inbox);

        await handler.Handle(notification, CancellationToken.None);

        var rows = await ctx.PlaceTranslations.AsNoTracking().ToListAsync();
        rows.Should().HaveCount(2);
        rows.Select(x => x.PlaceId).Should().BeEquivalentTo(new[] { p1.Id, p2.Id });
        rows.Should().OnlyContain(x => x.LanguageId == notification.Event.LanguageId);
        rows.Should().OnlyContain(x => x.Name.EndsWith("-fr", StringComparison.Ordinal));
    }

    // ── 3. Does not duplicate existing PlaceTranslation rows ──────────────────

    [Fact]
    public async Task LanguageActivated_DoesNotDuplicateExistingPlaceTranslations()
    {
        await using var ctx = NewInMemoryContext();

        var languageId = Guid.NewGuid();

        var existing = SeedPlace(ctx);
        var missing  = SeedPlace(ctx);
        await ctx.SaveChangesAsync();

        // Pre-seed a translation for `existing` so the anti-join must skip it.
        ctx.PlaceTranslations.Add(PlaceTranslation.Create(
            existing.Id, languageId, "preexisting", "desc", "address"));
        await ctx.SaveChangesAsync();

        var notification = NewNotification(languageId: languageId, languageCode: "de");
        var orchestrator = BuildOrchestrator();
        var uow          = new CountingUnitOfWork(ctx);
        var inbox        = new TestInboxStore(ctx);
        var handler      = BuildHandler(ctx, orchestrator, uow, inbox);

        await handler.Handle(notification, CancellationToken.None);

        var rows = await ctx.PlaceTranslations.AsNoTracking().ToListAsync();
        rows.Should().HaveCount(2,
            "the pre-existing translation must remain and exactly one new translation must be added");

        rows.Where(r => r.PlaceId == existing.Id).Should().ContainSingle()
            .Which.Name.Should().Be("preexisting");
        rows.Where(r => r.PlaceId == missing.Id).Should().ContainSingle()
            .Which.LanguageId.Should().Be(languageId);

        // Orchestrator called only for the missing Place.
        await orchestrator.Received(1).TranslateAsync(
            Arg.Any<IReadOnlyDictionary<string, string>>(),
            Arg.Any<string>(),
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<CancellationToken>());
    }

    // ── 4. Adds missing BusinessTranslation rows ──────────────────────────────

    [Fact]
    public async Task LanguageActivated_AddsMissingBusinessTranslations()
    {
        await using var ctx = NewInMemoryContext();

        var b1 = SeedBusiness(ctx);
        var b2 = SeedBusiness(ctx);
        await ctx.SaveChangesAsync();

        var notification = NewNotification(languageCode: "ar");
        var orchestrator = BuildOrchestrator();
        var uow          = new CountingUnitOfWork(ctx);
        var inbox        = new TestInboxStore(ctx);
        var handler      = BuildHandler(ctx, orchestrator, uow, inbox);

        await handler.Handle(notification, CancellationToken.None);

        var rows = await ctx.BusinessTranslations.AsNoTracking().ToListAsync();
        rows.Should().HaveCount(2);
        rows.Select(x => x.BusinessId).Should().BeEquivalentTo(new[] { b1.Id, b2.Id });
        rows.Should().OnlyContain(x => x.LanguageId == notification.Event.LanguageId);
        rows.Should().OnlyContain(x => x.Name.EndsWith("-ar", StringComparison.Ordinal));
    }

    // ── 5. Does not duplicate existing BusinessTranslation rows ───────────────

    [Fact]
    public async Task LanguageActivated_DoesNotDuplicateExistingBusinessTranslations()
    {
        await using var ctx = NewInMemoryContext();

        var languageId = Guid.NewGuid();

        var existing = SeedBusiness(ctx);
        var missing  = SeedBusiness(ctx);
        await ctx.SaveChangesAsync();

        ctx.BusinessTranslations.Add(BusinessTranslation.Create(
            existing.Id, languageId, "preexisting", "desc", "address"));
        await ctx.SaveChangesAsync();

        var notification = NewNotification(languageId: languageId, languageCode: "es");
        var orchestrator = BuildOrchestrator();
        var uow          = new CountingUnitOfWork(ctx);
        var inbox        = new TestInboxStore(ctx);
        var handler      = BuildHandler(ctx, orchestrator, uow, inbox);

        await handler.Handle(notification, CancellationToken.None);

        var rows = await ctx.BusinessTranslations.AsNoTracking().ToListAsync();
        rows.Should().HaveCount(2);

        rows.Where(r => r.BusinessId == existing.Id).Should().ContainSingle()
            .Which.Name.Should().Be("preexisting");
        rows.Where(r => r.BusinessId == missing.Id).Should().ContainSingle()
            .Which.LanguageId.Should().Be(languageId);

        await orchestrator.Received(1).TranslateAsync(
            Arg.Any<IReadOnlyDictionary<string, string>>(),
            Arg.Any<string>(),
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<CancellationToken>());
    }

    // ── 6. Does NOT load full graph (no tracked Place / Business) ─────────────

    [Fact]
    public async Task LanguageActivated_AvoidsLoadingFullGraph()
    {
        await using var ctx = NewInMemoryContext();

        SeedPlace(ctx);
        SeedBusiness(ctx);
        await ctx.SaveChangesAsync();

        // Clear so we can assert the handler does NOT pull aggregates into
        // tracked state via Include().
        ctx.ChangeTracker.Clear();

        var notification = NewNotification();
        var orchestrator = BuildOrchestrator();
        var uow          = new CountingUnitOfWork(ctx);
        var inbox        = new TestInboxStore(ctx);
        var handler      = BuildHandler(ctx, orchestrator, uow, inbox);

        await handler.Handle(notification, CancellationToken.None);

        ctx.ChangeTracker.Entries<Place>().Should().BeEmpty(
            "the projection-first handler must NOT load Place aggregates into the change tracker");
        ctx.ChangeTracker.Entries<Business>().Should().BeEmpty(
            "the projection-first handler must NOT load Business aggregates into the change tracker");

        // Sanity: the work still happened.
        (await ctx.PlaceTranslations.CountAsync()).Should().Be(1);
        (await ctx.BusinessTranslations.CountAsync()).Should().Be(1);
    }

    // ── 7. Processes in batches: SaveChanges count == ⌈P/200⌉ + ⌈B/200⌉ + 1 ───

    [Fact]
    public async Task LanguageActivated_ProcessesInBatches()
    {
        await using var ctx = NewInMemoryContext();

        // Seed 250 Places (→ 2 Place batches), 50 Businesses (→ 1 Business batch).
        // Expected SaveChanges via UoW: 2 + 1 + 1 (final inbox flush) = 4.
        const int placeCount    = 250;
        const int businessCount = 50;

        for (var i = 0; i < placeCount; i++)
        {
            SeedPlace(ctx);
        }
        for (var i = 0; i < businessCount; i++)
        {
            SeedBusiness(ctx);
        }
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var notification = NewNotification();
        var orchestrator = BuildOrchestrator();
        var uow          = new CountingUnitOfWork(ctx);
        var inbox        = new TestInboxStore(ctx);
        var handler      = BuildHandler(ctx, orchestrator, uow, inbox);

        await handler.Handle(notification, CancellationToken.None);

        var expectedSaves =
            ((placeCount    + BatchSize - 1) / BatchSize) +
            ((businessCount + BatchSize - 1) / BatchSize) +
            1; // final inbox flush
        uow.SaveCount.Should().Be(expectedSaves,
            $"two Place batches (250→2) + one Business batch (50→1) + one final inbox flush = {expectedSaves} saves");

        (await ctx.PlaceTranslations.CountAsync()).Should().Be(placeCount);
        (await ctx.BusinessTranslations.CountAsync()).Should().Be(businessCount);
        (await ctx.Set<InboxMessage>().AnyAsync(m => m.Id == notification.MessageId)).Should().BeTrue();
    }

    // ── 8. Inbox marked processed ONLY after every batch succeeds ─────────────

    [Fact]
    public async Task LanguageActivated_MarksInboxProcessedOnlyAfterSuccess()
    {
        await using var ctx = NewInMemoryContext();

        // Seed 300 Places so we have 2 Place batches; force the SECOND save to throw.
        for (var i = 0; i < 300; i++)
        {
            SeedPlace(ctx);
        }
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var notification = NewNotification();
        var orchestrator = BuildOrchestrator();
        var uow          = new CountingUnitOfWork(ctx)
        {
            FailOnSave = saveNumber => saveNumber == 2
                ? new InvalidOperationException("simulated mid-batch failure")
                : null,
        };
        var inbox   = new TestInboxStore(ctx);
        var handler = BuildHandler(ctx, orchestrator, uow, inbox);

        var act = async () => await handler.Handle(notification, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*simulated mid-batch failure*");

        // First batch's translations were already committed by the InMemory
        // provider → 200 rows present.
        (await ctx.PlaceTranslations.CountAsync()).Should().Be(200,
            "the first batch's translations should be durable on the InMemory store");

        // Inbox row must NOT have been written — replay must converge.
        var inboxExists = await ctx.Set<InboxMessage>()
            .AnyAsync(m => m.Id == notification.MessageId);
        inboxExists.Should().BeFalse(
            "MarkAsProcessed must run AFTER all batches succeed; a mid-batch failure must leave the inbox unmarked so replay re-processes the remaining work");
    }

    // ── 9. Orchestrator-empty: throw, do NOT mark inbox, no infinite loop ─────

    /// <summary>
    /// Critical correctness fix vs. the previous handler: when the orchestrator
    /// returns an empty translated-set, the handler must NOT silently skip the
    /// row and STILL mark the inbox processed at the end.  That combination
    /// permanently lost translations.  The new handler must throw immediately
    /// so the inbox stays unmarked and replay can retry.
    /// </summary>
    [Fact]
    public async Task LanguageActivated_OrchestratorReturnsEmptySet_ThrowsAndDoesNotMarkInbox()
    {
        await using var ctx = NewInMemoryContext();
        SeedPlace(ctx);
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        // Orchestrator that returns an empty translated-sets list — simulates a
        // translation backend that came back with no usable result.
        var orchestrator = Substitute.For<IEntityTranslationOrchestrator>();
        orchestrator
            .TranslateAsync(
                Arg.Any<IReadOnlyDictionary<string, string>>(),
                Arg.Any<string>(),
                Arg.Any<IReadOnlyList<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<EntityFieldTranslationSet>>(
                Array.Empty<EntityFieldTranslationSet>()));

        var notification = NewNotification();
        var uow          = new CountingUnitOfWork(ctx);
        var inbox        = new TestInboxStore(ctx);
        var handler      = BuildHandler(ctx, orchestrator, uow, inbox);

        // Hard wall-clock timeout so any infinite-loop hazard fails loudly.
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var act = async () => await handler.Handle(notification, cts.Token);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*returned no translated set*");

        var inboxRow = await ctx.Set<InboxMessage>()
            .AnyAsync(m => m.Id == notification.MessageId);
        inboxRow.Should().BeFalse(
            "the orchestrator-empty failure must leave the inbox unmarked so replay can retry");

        (await ctx.PlaceTranslations.CountAsync()).Should().Be(0,
            "the failing Place must not have produced a translation row");
    }
}
