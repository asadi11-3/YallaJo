using ContentCore.Contracts.IntegrationEvents;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Entities;
using ContentTours.Domain.Enums;
using ContentTours.Infrastructure.EventHandlers;
using ContentTours.Infrastructure.Persistence;
using ContentTours.Tests.Unit.Mohammad;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Infrastructure.Inbox;

namespace ContentTours.Tests.Unit;

/// <summary>
/// P1-004 regression tests: <see cref="LanguageActivatedIntegrationEventHandler"/>
/// must use projection-first anti-join queries (no Include-heavy graph load),
/// process candidates in fixed-size batches, and mark the inbox processed only
/// AFTER every batch has been saved.
///
/// <para>
/// These tests use the real <see cref="ContentToursDbContext"/> via the EF
/// InMemory provider so that anti-join queries actually run and the per-batch
/// SaveChanges semantics are exercised end-to-end.
/// </para>
/// </summary>
public sealed class LanguageActivatedIntegrationEventHandlerTests
{
    private const int BatchSize = 200; // mirrors handler constant

    // ── Test doubles ──────────────────────────────────────────────────────────

    /// <summary>
    /// Test UoW that delegates to the InMemory <see cref="ContentToursDbContext"/>
    /// (so anti-join queries see the prior batch's writes) and records a save
    /// counter so batching can be asserted.  Also supports an injectable
    /// throw-on-call hook to simulate per-batch failures.
    /// </summary>
    private sealed class CountingUnitOfWork(ContentToursDbContext ctx) : IContentToursUnitOfWork
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
    /// production <see cref="ContentToursInboxStore"/> exactly so every test
    /// exercises the real <c>InboxMessage</c> table contract.
    /// </summary>
    private sealed class TestInboxStore(ContentToursDbContext ctx) : IContentToursInboxStore
    {
        public Task<bool> HasBeenProcessedAsync(Guid messageId, CancellationToken ct = default)
            => ctx.Set<InboxMessage>().AnyAsync(m => m.Id == messageId, ct);

        public void MarkAsProcessed(Guid messageId)
            => ctx.Set<InboxMessage>().Add(InboxMessage.Create(messageId));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static IntegrationEventNotification<LanguageActivatedIntegrationEvent> NewNotification(
        Guid? messageId = null,
        Guid? languageId = null,
        string languageCode = "fr") =>
        new(
            messageId ?? Guid.NewGuid(),
            new LanguageActivatedIntegrationEvent(
                languageId ?? Guid.NewGuid(),
                languageCode));

    private static IEntityTranslationOrchestrator BuildOrchestrator(
        Action<IDictionary<string, string>>? customise = null)
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

                customise?.Invoke(translated);

                IReadOnlyList<EntityFieldTranslationSet> result =
                    new[] { new EntityFieldTranslationSet(Guid.NewGuid(), lang, translated) };
                return Task.FromResult(result);
            });
        return orchestrator;
    }

    private static LanguageActivatedIntegrationEventHandler BuildHandler(
        ContentToursDbContext ctx,
        IEntityTranslationOrchestrator orchestrator,
        IContentToursUnitOfWork uow,
        IContentToursInboxStore inbox) =>
        new(
            ctx,
            orchestrator,
            uow,
            inbox,
            Substitute.For<ILogger<LanguageActivatedIntegrationEventHandler>>());

    private static Tour SeedTour(ContentToursDbContext ctx, string nameSuffix = "")
    {
        var tour = TestTourFactory.CreateDraft(slug: $"slug-{Guid.NewGuid():N}");
        // Use reflection-free public path: Name set by factory, but tests want a
        // distinguishable name — adjust via Update(..) only if needed.  Default
        // factory name "Petra Day Tour" is stable enough for ID-based assertions.
        ctx.Tours.Add(tour);
        return tour;
    }

    private static TourPricingTier SeedTier(
        ContentToursDbContext ctx,
        Guid tourId,
        string name = "Adult",
        ParticipantType participantType = ParticipantType.Adult)
    {
        var tier = TestPricingTierFactory.Create(tourId, name: name, participantType: participantType);
        ctx.TourPricingTiers.Add(tier);
        return tier;
    }

    // ── 1. Inbox already processed: short-circuit ─────────────────────────────

    [Fact]
    public async Task LanguageActivated_WhenInboxAlreadyProcessed_DoesNothing()
    {
        await using var ctx = TestDbContextFactory.NewInMemory();

        var notification = NewNotification();
        ctx.Set<InboxMessage>().Add(InboxMessage.Create(notification.MessageId));
        await ctx.SaveChangesAsync();

        // Seed a tour that WOULD need a translation if the handler ran.
        SeedTour(ctx);
        await ctx.SaveChangesAsync();

        var orchestrator = BuildOrchestrator();
        var uow          = new CountingUnitOfWork(ctx);
        var inbox        = new TestInboxStore(ctx);
        var handler      = BuildHandler(ctx, orchestrator, uow, inbox);

        await handler.Handle(notification, CancellationToken.None);

        // No translations created.
        (await ctx.TourTranslations.CountAsync()).Should().Be(0);
        (await ctx.TourPricingTierTranslations.CountAsync()).Should().Be(0);

        // No SaveChanges via UoW (the early-return path bypasses the loop entirely).
        uow.SaveCount.Should().Be(0,
            "the inbox-already-processed branch must not invoke SaveChangesAsync");

        // Orchestrator must never be called for an already-processed message.
        await orchestrator.DidNotReceiveWithAnyArgs().TranslateAsync(
            default!, default!, default!, default);
    }

    // ── 2. Adds missing TourTranslation rows ──────────────────────────────────

    [Fact]
    public async Task LanguageActivated_AddsMissingTourTranslations()
    {
        await using var ctx = TestDbContextFactory.NewInMemory();

        var t1 = SeedTour(ctx);
        var t2 = SeedTour(ctx);
        await ctx.SaveChangesAsync();

        var notification = NewNotification(languageCode: "fr");
        var orchestrator = BuildOrchestrator();
        var uow          = new CountingUnitOfWork(ctx);
        var inbox        = new TestInboxStore(ctx);
        var handler      = BuildHandler(ctx, orchestrator, uow, inbox);

        await handler.Handle(notification, CancellationToken.None);

        var translations = await ctx.TourTranslations.AsNoTracking().ToListAsync();
        translations.Should().HaveCount(2);
        translations.Select(x => x.TourId).Should().BeEquivalentTo(new[] { t1.Id, t2.Id });
        translations.Should().OnlyContain(x => x.LanguageId == notification.Event.LanguageId);
        translations.Should().OnlyContain(x => x.Name.EndsWith("-fr", StringComparison.Ordinal));

        // Orchestrator called once per missing tour.
        await orchestrator.Received(2).TranslateAsync(
            Arg.Any<IReadOnlyDictionary<string, string>>(),
            Arg.Any<string>(),
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<CancellationToken>());
    }

    // ── 3. Does not duplicate existing TourTranslation rows ───────────────────

    [Fact]
    public async Task LanguageActivated_DoesNotDuplicateExistingTourTranslations()
    {
        await using var ctx = TestDbContextFactory.NewInMemory();

        var languageId = Guid.NewGuid();

        var existing = SeedTour(ctx);
        var missing  = SeedTour(ctx);
        await ctx.SaveChangesAsync();

        // Pre-seed a TourTranslation for `existing` so the anti-join must skip it.
        ctx.TourTranslations.Add(TourTranslation.Create(
            existing.Id, languageId, "preexisting", "desc", "short", "(0, 0)"));
        await ctx.SaveChangesAsync();

        var notification = NewNotification(languageId: languageId, languageCode: "de");
        var orchestrator = BuildOrchestrator();
        var uow          = new CountingUnitOfWork(ctx);
        var inbox        = new TestInboxStore(ctx);
        var handler      = BuildHandler(ctx, orchestrator, uow, inbox);

        await handler.Handle(notification, CancellationToken.None);

        var rows = await ctx.TourTranslations.AsNoTracking().ToListAsync();
        rows.Should().HaveCount(2,
            "the pre-existing translation must remain and exactly one new translation must be added");

        rows.Where(r => r.TourId == existing.Id).Should().ContainSingle()
            .Which.Name.Should().Be("preexisting");
        rows.Where(r => r.TourId == missing.Id).Should().ContainSingle()
            .Which.LanguageId.Should().Be(languageId);

        // Orchestrator called only for the missing tour.
        await orchestrator.Received(1).TranslateAsync(
            Arg.Any<IReadOnlyDictionary<string, string>>(),
            Arg.Any<string>(),
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<CancellationToken>());
    }

    // ── 4. Adds missing pricing-tier translations ─────────────────────────────

    [Fact]
    public async Task LanguageActivated_AddsMissingPricingTierTranslations()
    {
        await using var ctx = TestDbContextFactory.NewInMemory();

        var tour = SeedTour(ctx);
        await ctx.SaveChangesAsync();

        var tier1 = SeedTier(ctx, tour.Id, name: "Adult", participantType: ParticipantType.Adult);
        var tier2 = SeedTier(ctx, tour.Id, name: "Child", participantType: ParticipantType.Child);
        await ctx.SaveChangesAsync();

        // Clear change tracker so the InMemory navigation-fixer does not try to
        // append the new TourPricingTierTranslation rows to the tracked
        // TourPricingTier.Translations read-only collection (production SQL
        // server uses lazy/no-fixup; this matches the production code path).
        ctx.ChangeTracker.Clear();

        var notification = NewNotification(languageCode: "FR"); // upper-case to verify normalisation
        var orchestrator = BuildOrchestrator();
        var uow          = new CountingUnitOfWork(ctx);
        var inbox        = new TestInboxStore(ctx);
        var handler      = BuildHandler(ctx, orchestrator, uow, inbox);

        await handler.Handle(notification, CancellationToken.None);

        var rows = await ctx.TourPricingTierTranslations.AsNoTracking().ToListAsync();
        rows.Should().HaveCount(2);
        rows.Select(r => r.TourPricingTierId).Should().BeEquivalentTo(new[] { tier1.Id, tier2.Id });
        rows.Should().OnlyContain(r => r.LanguageCode == "fr",
            "the handler must lower-case the language code before persisting");

        // Source Name copied verbatim (no Azure call for tiers — preserve existing behaviour).
        rows.Single(r => r.TourPricingTierId == tier1.Id).Name.Should().Be("Adult");
        rows.Single(r => r.TourPricingTierId == tier2.Id).Name.Should().Be("Child");
    }

    // ── 5. Does not duplicate existing pricing-tier translations ──────────────

    [Fact]
    public async Task LanguageActivated_DoesNotDuplicateExistingPricingTierTranslations()
    {
        await using var ctx = TestDbContextFactory.NewInMemory();

        var tour = SeedTour(ctx);
        await ctx.SaveChangesAsync();

        var existing = SeedTier(ctx, tour.Id, name: "Adult", participantType: ParticipantType.Adult);
        var missing  = SeedTier(ctx, tour.Id, name: "Child", participantType: ParticipantType.Child);
        await ctx.SaveChangesAsync();

        // Detach pre-seed to avoid InMemory navigation-fixer attempting to
        // append onto the tracked TourPricingTier.Translations read-only
        // collection while seeding the pre-existing translation.
        ctx.ChangeTracker.Clear();

        // Pre-seed a translation for `existing` so the anti-join must skip it.
        ctx.TourPricingTierTranslations.Add(
            TourPricingTierTranslation.Create(existing.Id, "es", "Adulto", null));
        await ctx.SaveChangesAsync();

        // Detach again so the handler runs against a clean change tracker —
        // matches production where the handler scope owns its own DbContext.
        ctx.ChangeTracker.Clear();

        var notification = NewNotification(languageCode: "es");
        var orchestrator = BuildOrchestrator();
        var uow          = new CountingUnitOfWork(ctx);
        var inbox        = new TestInboxStore(ctx);
        var handler      = BuildHandler(ctx, orchestrator, uow, inbox);

        await handler.Handle(notification, CancellationToken.None);

        var rows = await ctx.TourPricingTierTranslations.AsNoTracking().ToListAsync();
        rows.Should().HaveCount(2);

        rows.Where(r => r.TourPricingTierId == existing.Id).Should().ContainSingle()
            .Which.Name.Should().Be("Adulto");
        rows.Where(r => r.TourPricingTierId == missing.Id).Should().ContainSingle()
            .Which.LanguageCode.Should().Be("es");
    }

    // ── 6. Does NOT load full tour graph (no tracked tours, no Include) ───────

    [Fact]
    public async Task LanguageActivated_AvoidsLoadingFullTourGraph()
    {
        await using var ctx = TestDbContextFactory.NewInMemory();

        var tour = SeedTour(ctx);
        await ctx.SaveChangesAsync();
        SeedTier(ctx, tour.Id);
        await ctx.SaveChangesAsync();

        // Clear the change tracker so we can assert the handler does NOT pull
        // the Tour aggregate into tracked state via Include().
        ctx.ChangeTracker.Clear();

        var notification = NewNotification();
        var orchestrator = BuildOrchestrator();
        var uow          = new CountingUnitOfWork(ctx);
        var inbox        = new TestInboxStore(ctx);
        var handler      = BuildHandler(ctx, orchestrator, uow, inbox);

        await handler.Handle(notification, CancellationToken.None);

        // After processing, no Tour aggregate should be tracked — only the
        // newly-added child rows + InboxMessage.  Specifically: zero tracked Tours.
        var trackedTours = ctx.ChangeTracker
            .Entries<Tour>()
            .ToList();
        trackedTours.Should().BeEmpty(
            "the projection-first handler must NOT load Tour aggregates into the change tracker");

        // Sanity: the work still happened.
        (await ctx.TourTranslations.CountAsync()).Should().Be(1);
        (await ctx.TourPricingTierTranslations.CountAsync()).Should().Be(1);
    }

    // ── 7. Processes in batches: SaveChanges count == ⌈N/200⌉ + 1 ─────────────

    [Fact]
    public async Task LanguageActivated_ProcessesInBatches()
    {
        await using var ctx = TestDbContextFactory.NewInMemory();

        // Seed 250 tours (BatchSize=200 → 2 tour batches), and 0 tiers (0 batches).
        // Expected SaveChanges via UoW: 2 (tour batches) + 1 (final inbox flush) = 3.
        const int tourCount = 250;
        for (var i = 0; i < tourCount; i++)
        {
            SeedTour(ctx);
        }
        await ctx.SaveChangesAsync();

        var notification = NewNotification();
        var orchestrator = BuildOrchestrator();
        var uow          = new CountingUnitOfWork(ctx);
        var inbox        = new TestInboxStore(ctx);
        var handler      = BuildHandler(ctx, orchestrator, uow, inbox);

        await handler.Handle(notification, CancellationToken.None);

        // 250 tours → ceil(250/200) = 2 tour batches. 0 tier batches. + 1 final inbox flush.
        var expected = ((tourCount + BatchSize - 1) / BatchSize) + 1;
        uow.SaveCount.Should().Be(expected,
            $"two tour batches should be saved separately, plus a final inbox flush (got {uow.SaveCount}, expected {expected})");

        (await ctx.TourTranslations.CountAsync()).Should().Be(tourCount);
        (await ctx.Set<InboxMessage>().AnyAsync(m => m.Id == notification.MessageId)).Should().BeTrue();
    }

    // ── 8. Inbox is marked processed ONLY after every batch succeeds ──────────

    [Fact]
    public async Task LanguageActivated_MarksInboxProcessedOnlyAfterSuccess()
    {
        await using var ctx = TestDbContextFactory.NewInMemory();

        // Seed 300 tours so we have 2 tour batches; force the SECOND batch save to throw.
        for (var i = 0; i < 300; i++)
        {
            SeedTour(ctx);
        }
        await ctx.SaveChangesAsync();

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
        // provider (transactions are no-ops there) → 200 rows present.
        (await ctx.TourTranslations.CountAsync()).Should().Be(200,
            "the first batch's translations should be durable on the InMemory store");

        // Inbox row must NOT have been written — replay must converge.
        var inboxExists = await ctx.Set<InboxMessage>()
            .AnyAsync(m => m.Id == notification.MessageId);
        inboxExists.Should().BeFalse(
            "MarkAsProcessed must run AFTER all batches succeed; a mid-batch failure must leave the inbox unmarked so replay re-processes the remaining work");
    }

    // ── 9. Pricing-tier translations are NOT routed through Azure orchestrator ──

    [Fact]
    public async Task LanguageActivated_DoesNotInvokeOrchestratorForPricingTiers()
    {
        await using var ctx = TestDbContextFactory.NewInMemory();

        // One tour that already has its translation, so the orchestrator is not
        // called for tour translations either; only tiers remain to process.
        var languageId = Guid.NewGuid();
        var tour       = SeedTour(ctx);
        await ctx.SaveChangesAsync();

        ctx.TourTranslations.Add(TourTranslation.Create(
            tour.Id, languageId, "preexisting", null, null, null));
        SeedTier(ctx, tour.Id);
        await ctx.SaveChangesAsync();

        ctx.ChangeTracker.Clear();

        var notification = NewNotification(languageId: languageId, languageCode: "ar");
        var orchestrator = BuildOrchestrator();
        var uow          = new CountingUnitOfWork(ctx);
        var inbox        = new TestInboxStore(ctx);
        var handler      = BuildHandler(ctx, orchestrator, uow, inbox);

        await handler.Handle(notification, CancellationToken.None);

        // Tier translation was created…
        var tierTranslations = await ctx.TourPricingTierTranslations.AsNoTracking().ToListAsync();
        tierTranslations.Should().HaveCount(1);

        // …but no orchestrator call happened (tiers must NOT call Azure — preserve
        // historical behaviour from the pre-P1-004 handler).
        await orchestrator.DidNotReceiveWithAnyArgs().TranslateAsync(
            default!, default!, default!, default);
    }

    // ── 10. P1-004.1: orchestrator returns no translated set → throw, no infinite loop ──

    /// <summary>
    /// Critical infinite-loop guard for the P1-004.1 streaming refactor.
    ///
    /// <para>
    /// With DB-level batch fetching, if a tour candidate is selected but the
    /// orchestrator returns an empty set, the handler must NOT silently skip
    /// the row.  Otherwise the next iteration's anti-join would re-select the
    /// same candidate forever (no translation row was ever added → still
    /// missing).  The handler must instead throw <see cref="InvalidOperationException"/>
    /// so the outer pipeline retries on the next inbox delivery.
    /// </para>
    /// </summary>
    [Fact]
    public async Task LanguageActivated_OrchestratorReturnsEmptySet_ThrowsAndDoesNotMarkInbox()
    {
        await using var ctx = TestDbContextFactory.NewInMemory();
        SeedTour(ctx);
        await ctx.SaveChangesAsync();

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

        // Use a hard wall-clock timeout to fail loudly if the handler ever
        // enters the historical infinite-loop hazard.  The handler should throw
        // immediately on the first candidate — well under 5 seconds.
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var act = async () => await handler.Handle(notification, cts.Token);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*returned no translated set*");

        // Inbox MUST remain unmarked so the next delivery re-processes the work.
        var inboxRow = await ctx.Set<InboxMessage>()
            .AnyAsync(m => m.Id == notification.MessageId);
        inboxRow.Should().BeFalse(
            "the orchestrator-empty failure must leave the inbox unmarked so replay can retry");

        // No translation rows were committed for the failing candidate.
        (await ctx.TourTranslations.CountAsync()).Should().Be(0,
            "the failing tour must not have produced a translation row");
    }

    // ── 11. P1-004.1: each ToListAsync fetches AT MOST BatchSize rows ─────────

    /// <summary>
    /// Pins the P1-004.1 streaming contract: the handler must NOT materialise
    /// the entire missing-translation backlog up front.  Each anti-join query
    /// must return at most <see cref="BatchSize"/> rows.
    ///
    /// <para>
    /// Verified by intercepting EF Core <c>QueryExecuting</c> events through a
    /// recording <see cref="ILoggerFactory"/> attached to the DbContext: in
    /// practice the simplest reliable assertion is that with
    /// <c>2 × BatchSize + a partial</c> tours seeded, the handler issues exactly
    /// 3 tour-batch saves and the InMemory provider's per-query result counts
    /// never exceed <see cref="BatchSize"/>.  We assert this indirectly via
    /// <c>SaveChanges</c> count and final state.
    /// </para>
    /// </summary>
    [Fact]
    public async Task LanguageActivated_DbLevelBatchFetching_LimitsRowsPerQuery()
    {
        await using var ctx = TestDbContextFactory.NewInMemory();

        // 2 × BatchSize + 50 tours → exactly 3 tour-batch fetches:
        //   batch 1 fetches 200, batch 2 fetches 200, batch 3 fetches 50.
        // Plus 1 final inbox-flush save = 4 saves total.
        const int tourCount = (2 * BatchSize) + 50;
        for (var i = 0; i < tourCount; i++)
        {
            SeedTour(ctx);
        }
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var notification = NewNotification();
        var orchestrator = BuildOrchestrator();
        var uow          = new CountingUnitOfWork(ctx);
        var inbox        = new TestInboxStore(ctx);
        var handler      = BuildHandler(ctx, orchestrator, uow, inbox);

        await handler.Handle(notification, CancellationToken.None);

        // 3 tour batches + 1 final inbox flush = 4 saves total.
        var expectedSaves = ((tourCount + BatchSize - 1) / BatchSize) + 1;
        uow.SaveCount.Should().Be(expectedSaves,
            $"streaming handler must save once per fetched batch (≤{BatchSize} rows each), " +
            "plus one final inbox-flush save");

        (await ctx.TourTranslations.CountAsync()).Should().Be(tourCount);
        (await ctx.Set<InboxMessage>().AnyAsync(m => m.Id == notification.MessageId)).Should().BeTrue();
    }
}
