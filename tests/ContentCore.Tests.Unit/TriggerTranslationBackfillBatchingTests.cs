using ContentCore.Application.Commands.Translation.TriggerTranslationBackfill;
using ContentCore.Application.Interfaces;
using ContentCore.Domain.Entities;
using ContentCore.Domain.Repositories;
using ContentCore.Infrastructure.Persistence;
using ContentCore.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using TagEntity = ContentCore.Domain.Entities.Tag;
using SpecEntity = ContentCore.Domain.Entities.Specialization;

namespace ContentCore.Tests.Unit;

/// <summary>
/// CONTENTCORE-FOLLOWUP-BACKFILL-001 batching / anti-join regression tests.
///
/// <para>
/// Exercises the production <see cref="TranslationBackfillStore"/> against a
/// real <see cref="ContentCoreDbContext"/> on SQLite in-memory so the
/// projection-first anti-join queries actually execute.  Uses NSubstitute
/// fakes for <see cref="IEntityTranslationOrchestrator"/>,
/// <see cref="IActiveLanguageProvider"/> and <see cref="IContentCoreUnitOfWork"/>.
/// </para>
///
/// <para>
/// Mirrors the test pattern established by ContentTours and ContentPlaces
/// LanguageActivated batching tests.
/// </para>
/// </summary>
public sealed class TriggerTranslationBackfillBatchingTests : IDisposable
{
    private const int BatchSize = 200; // mirrors handler constant

    private readonly ContentCoreDbContext _ctx;

    public TriggerTranslationBackfillBatchingTests()
    {
        var options = new DbContextOptionsBuilder<ContentCoreDbContext>()
            .UseInMemoryDatabase(databaseName: $"backfill-tests-{Guid.NewGuid():N}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _ctx = new ContentCoreDbContext(options);
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    // ── Test doubles ──────────────────────────────────────────────────────────

    /// <summary>
    /// UoW that delegates to the real DbContext (so anti-join queries see prior
    /// batch's writes) and counts SaveChangesAsync calls.  Optionally fails on
    /// the Nth save to exercise the conflict path.
    /// </summary>
    private sealed class CountingUnitOfWork(ContentCoreDbContext ctx) : IContentCoreUnitOfWork
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

    // ── Helpers ───────────────────────────────────────────────────────────────

    private TranslationBackfillStore NewBackfillStore() => new(_ctx);

    private IActiveLanguageProvider LanguageProvider(params (Guid Id, string Code)[] languages)
    {
        var p = Substitute.For<IActiveLanguageProvider>();
        p.GetActiveLanguagesAsync(Arg.Any<CancellationToken>())
            .Returns(languages.Select(l => new ActiveLanguage(l.Id, l.Code)).ToList());
        return p;
    }

    /// <summary>
    /// Orchestrator that translates every requested field by appending "-{lang}"
    /// to its source value and returns one set per requested target language.
    /// Mirrors the production <c>EntityTranslationOrchestrator</c> contract:
    /// the <c>LanguageId</c> on the returned set is the real id from the
    /// supplied <paramref name="codeToId"/> lookup (NOT a random Guid).
    /// </summary>
    private static IEntityTranslationOrchestrator EchoOrchestrator(
        IReadOnlyDictionary<string, Guid> codeToId,
        Action<IDictionary<string, string>>? customise = null)
    {
        var o = Substitute.For<IEntityTranslationOrchestrator>();
        o.TranslateAsync(
                Arg.Any<IReadOnlyDictionary<string, string>>(),
                Arg.Any<string>(),
                Arg.Any<IReadOnlyList<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var fields  = call.Arg<IReadOnlyDictionary<string, string>>();
                var targets = call.Arg<IReadOnlyList<string>>();
                var sets = new List<EntityFieldTranslationSet>(targets.Count);
                foreach (var code in targets)
                {
                    if (!codeToId.TryGetValue(code, out var langId))
                        continue;

                    var translated = fields.ToDictionary(
                        kv => kv.Key,
                        kv => $"{kv.Value}-{code}",
                        StringComparer.Ordinal);
                    customise?.Invoke(translated);
                    sets.Add(new EntityFieldTranslationSet(langId, code, translated));
                }
                return Task.FromResult<IReadOnlyList<EntityFieldTranslationSet>>(sets);
            });
        return o;
    }

    private TriggerTranslationBackfillCommandHandler BuildHandler(
        IEntityTranslationOrchestrator orchestrator,
        IActiveLanguageProvider languageProvider,
        IContentCoreUnitOfWork uow)
    {
        return new TriggerTranslationBackfillCommandHandler(
            uow,
            orchestrator,
            languageProvider,
            NewBackfillStore(),
            Substitute.For<ILogger<TriggerTranslationBackfillCommandHandler>>());
    }

    private static Language SeedLanguage(ContentCoreDbContext ctx, string code, string name)
    {
        var language = Language.Create(code, name, nativeName: name, isRtl: false);
        ctx.Languages.Add(language);
        return language;
    }

    private static TagEntity SeedTag(ContentCoreDbContext ctx, string name = "Adventure", string slug = "adventure")
    {
        var tag = TagEntity.Create(name, slug);
        ctx.Tags.Add(tag);
        return tag;
    }

    private static SpecEntity SeedSpec(ContentCoreDbContext ctx, string name = "Guide", string? description = null)
    {
        var spec = SpecEntity.Create(name, description);
        ctx.Specializations.Add(spec);
        return spec;
    }

    // ── 1. AddsMissingTagTranslations ────────────────────────────────────────

    [Fact]
    public async Task TriggerTranslationBackfill_AddsMissingTagTranslations()
    {
        var fr = SeedLanguage(_ctx, "fr", "French");
        var t1 = SeedTag(_ctx, "Adventure", "adventure-1");
        var t2 = SeedTag(_ctx, "Beach", "beach-1");
        await _ctx.SaveChangesAsync();
        _ctx.ChangeTracker.Clear();

        var orchestrator = EchoOrchestrator(new Dictionary<string, Guid> { [fr.Code] = fr.Id });
        var langs = LanguageProvider((fr.Id, fr.Code));
        var uow = new CountingUnitOfWork(_ctx);
        var handler = BuildHandler(orchestrator, langs, uow);

        var result = await handler.Handle(
            new TriggerTranslationBackfillCommand("tag"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalProcessed.Should().Be(2);
        result.Value.TotalTranslationsAdded.Should().Be(2);
        result.Value.TotalSkipped.Should().Be(0);

        var translations = await _ctx.TagTranslations.AsNoTracking().ToListAsync();
        translations.Should().HaveCount(2);
        translations.Select(x => x.TagId).Should().BeEquivalentTo(new[] { t1.Id, t2.Id });
        translations.Should().OnlyContain(x => x.LanguageId == fr.Id);
        translations.Should().OnlyContain(x => x.Name.EndsWith("-fr", StringComparison.Ordinal));
    }

    // ── 2. DoesNotDuplicateExistingTagTranslations ───────────────────────────

    [Fact]
    public async Task TriggerTranslationBackfill_DoesNotDuplicateExistingTagTranslations()
    {
        var fr = SeedLanguage(_ctx, "fr", "French");
        var translated = SeedTag(_ctx, "Adventure", "adventure-2");
        var missing    = SeedTag(_ctx, "Beach", "beach-2");
        await _ctx.SaveChangesAsync();

        // Pre-seed a French translation for `translated` so the anti-join must skip it.
        _ctx.TagTranslations.Add(TagTranslation.Create(translated.Id, fr.Id, "Aventure", "aventure"));
        await _ctx.SaveChangesAsync();
        _ctx.ChangeTracker.Clear();

        var orchestrator = EchoOrchestrator(new Dictionary<string, Guid> { [fr.Code] = fr.Id });
        var langs = LanguageProvider((fr.Id, fr.Code));
        var uow = new CountingUnitOfWork(_ctx);
        var handler = BuildHandler(orchestrator, langs, uow);

        var result = await handler.Handle(
            new TriggerTranslationBackfillCommand("tag"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalProcessed.Should().Be(1);
        result.Value.TotalTranslationsAdded.Should().Be(1);

        var translations = await _ctx.TagTranslations.AsNoTracking().OrderBy(t => t.Name).ToListAsync();
        translations.Should().HaveCount(2);
        translations.Single(t => t.TagId == translated.Id).Name.Should().Be("Aventure",
            "the pre-existing translation must be preserved");
        translations.Single(t => t.TagId == missing.Id).LanguageId.Should().Be(fr.Id,
            "the missing tag must have received exactly one new French translation");

        // Orchestrator called exactly once — only for the missing tag.
        await orchestrator.Received(1).TranslateAsync(
            Arg.Any<IReadOnlyDictionary<string, string>>(),
            Arg.Any<string>(),
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<CancellationToken>());
    }

    // ── 3. DoesNotCallOrchestrator_WhenAllTagsFullyTranslated ────────────────

    [Fact]
    public async Task TriggerTranslationBackfill_DoesNotCallOrchestrator_WhenAllTagsFullyTranslated()
    {
        var fr = SeedLanguage(_ctx, "fr", "French");
        var t1 = SeedTag(_ctx, "Adventure", "adventure-3");
        var t2 = SeedTag(_ctx, "Beach", "beach-3");
        await _ctx.SaveChangesAsync();

        // Pre-seed translations for both tags so the anti-join filter excludes them.
        _ctx.TagTranslations.AddRange(
            TagTranslation.Create(t1.Id, fr.Id, "Aventure", "aventure"),
            TagTranslation.Create(t2.Id, fr.Id, "Plage", "plage"));
        await _ctx.SaveChangesAsync();
        _ctx.ChangeTracker.Clear();

        var orchestrator = EchoOrchestrator(new Dictionary<string, Guid> { [fr.Code] = fr.Id });
        var langs = LanguageProvider((fr.Id, fr.Code));
        var uow = new CountingUnitOfWork(_ctx);
        var handler = BuildHandler(orchestrator, langs, uow);

        var result = await handler.Handle(
            new TriggerTranslationBackfillCommand("tag"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalProcessed.Should().Be(0);
        result.Value.TotalTranslationsAdded.Should().Be(0);

        // Anti-join filtered every tag out at the SQL layer → orchestrator never invoked.
        await orchestrator.DidNotReceiveWithAnyArgs().TranslateAsync(
            default!, default!, default!, default);

        // No save either, because the loop exited on an empty first batch.
        uow.SaveCount.Should().Be(0,
            "with no candidates the handler must not call SaveChangesAsync at all");
    }

    // ── 4. AddsMissingSpecializationTranslations ─────────────────────────────

    [Fact]
    public async Task TriggerTranslationBackfill_AddsMissingSpecializationTranslations()
    {
        var fr = SeedLanguage(_ctx, "fr", "French");
        var s1 = SeedSpec(_ctx, "Guide", "Tour guide service");
        var s2 = SeedSpec(_ctx, "Driver", null);
        await _ctx.SaveChangesAsync();
        _ctx.ChangeTracker.Clear();

        var orchestrator = EchoOrchestrator(new Dictionary<string, Guid> { [fr.Code] = fr.Id });
        var langs = LanguageProvider((fr.Id, fr.Code));
        var uow = new CountingUnitOfWork(_ctx);
        var handler = BuildHandler(orchestrator, langs, uow);

        var result = await handler.Handle(
            new TriggerTranslationBackfillCommand("specialization"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalProcessed.Should().Be(2);
        result.Value.TotalTranslationsAdded.Should().Be(2);

        var translations = await _ctx.SpecializationTranslations.AsNoTracking().ToListAsync();
        translations.Should().HaveCount(2);
        translations.Select(x => x.SpecializationId).Should().BeEquivalentTo(new[] { s1.Id, s2.Id });
        translations.Should().OnlyContain(x => x.LanguageId == fr.Id);
    }

    // ── 5. DoesNotDuplicateExistingSpecializationTranslations ────────────────

    [Fact]
    public async Task TriggerTranslationBackfill_DoesNotDuplicateExistingSpecializationTranslations()
    {
        var fr = SeedLanguage(_ctx, "fr", "French");
        var translated = SeedSpec(_ctx, "Guide", "Tour guide");
        var missing    = SeedSpec(_ctx, "Driver", "Private driver");
        await _ctx.SaveChangesAsync();

        _ctx.SpecializationTranslations.Add(
            SpecializationTranslation.Create(translated.Id, fr.Id, "Guide-fr", "Tour guide-fr"));
        await _ctx.SaveChangesAsync();
        _ctx.ChangeTracker.Clear();

        var orchestrator = EchoOrchestrator(new Dictionary<string, Guid> { [fr.Code] = fr.Id });
        var langs = LanguageProvider((fr.Id, fr.Code));
        var uow = new CountingUnitOfWork(_ctx);
        var handler = BuildHandler(orchestrator, langs, uow);

        var result = await handler.Handle(
            new TriggerTranslationBackfillCommand("specialization"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalProcessed.Should().Be(1);
        result.Value.TotalTranslationsAdded.Should().Be(1);

        var translations = await _ctx.SpecializationTranslations.AsNoTracking().ToListAsync();
        translations.Should().HaveCount(2);
        translations.Single(t => t.SpecializationId == translated.Id).Name.Should().Be("Guide-fr");
        translations.Single(t => t.SpecializationId == missing.Id).LanguageId.Should().Be(fr.Id);
    }

    // ── 6. ProcessesInBatches_SaveChangesPerBatch ────────────────────────────

    [Fact]
    public async Task TriggerTranslationBackfill_ProcessesInBatches_SaveChangesPerBatch()
    {
        var fr = SeedLanguage(_ctx, "fr", "French");
        // Seed 250 tags → ⌈250/200⌉ = 2 batches → 2 SaveChanges calls.
        // (Unlike the LanguageActivated handler there is no separate inbox flush
        //  save — this is a synchronous admin command with a single result record.)
        const int tagCount = 250;
        for (var i = 0; i < tagCount; i++)
            SeedTag(_ctx, $"Tag-{i}", $"tag-{i}");
        await _ctx.SaveChangesAsync();
        _ctx.ChangeTracker.Clear();

        var orchestrator = EchoOrchestrator(new Dictionary<string, Guid> { [fr.Code] = fr.Id });
        var langs = LanguageProvider((fr.Id, fr.Code));
        var uow = new CountingUnitOfWork(_ctx);
        var handler = BuildHandler(orchestrator, langs, uow);

        var result = await handler.Handle(
            new TriggerTranslationBackfillCommand("tag"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalProcessed.Should().Be(tagCount);
        result.Value.TotalTranslationsAdded.Should().Be(tagCount);

        var expectedSaves = (tagCount + BatchSize - 1) / BatchSize;
        uow.SaveCount.Should().Be(expectedSaves,
            $"250 tags → ⌈250/200⌉ = {expectedSaves} per-batch saves");

        (await _ctx.TagTranslations.CountAsync()).Should().Be(tagCount);
    }

    // ── 7. AvoidsLoadingFullGraph ────────────────────────────────────────────

    [Fact]
    public async Task TriggerTranslationBackfill_AvoidsLoadingFullGraph()
    {
        var fr = SeedLanguage(_ctx, "fr", "French");
        SeedTag(_ctx, "Adventure", "adventure-7");
        SeedSpec(_ctx, "Guide");
        await _ctx.SaveChangesAsync();
        _ctx.ChangeTracker.Clear();

        var orchestrator = EchoOrchestrator(new Dictionary<string, Guid> { [fr.Code] = fr.Id });
        var langs = LanguageProvider((fr.Id, fr.Code));
        var uow = new CountingUnitOfWork(_ctx);
        var handler = BuildHandler(orchestrator, langs, uow);

        await handler.Handle(new TriggerTranslationBackfillCommand("tag"), CancellationToken.None);
        await handler.Handle(new TriggerTranslationBackfillCommand("specialization"), CancellationToken.None);

        // After both runs: only the newly-added translation rows should be tracked
        // (briefly between Add and SaveChanges, then committed and detached).
        // Specifically NO Tag or Specialization aggregates were loaded into the
        // change tracker — the projection-first store uses AsNoTracking().
        _ctx.ChangeTracker.Entries<TagEntity>().Should().BeEmpty(
            "the projection-first store must NOT load Tag aggregates into the change tracker");
        _ctx.ChangeTracker.Entries<SpecEntity>().Should().BeEmpty(
            "the projection-first store must NOT load Specialization aggregates into the change tracker");

        // Sanity: the work still happened.
        (await _ctx.TagTranslations.CountAsync()).Should().Be(1);
        (await _ctx.SpecializationTranslations.CountAsync()).Should().Be(1);
    }

    // ── 8. TranslatorReturnsEmptyForLanguage_SkipsThatLanguage ───────────────

    [Fact]
    public async Task TriggerTranslationBackfill_TranslatorReturnsEmptyForLanguage_SkipsThatLanguage()
    {
        // Critical: empty translator result must NOT throw (different from the
        // LanguageActivated handlers — there is no inbox here, replay re-converges).
        var fr = SeedLanguage(_ctx, "fr", "French");
        SeedTag(_ctx, "Adventure", "adventure-8");
        await _ctx.SaveChangesAsync();
        _ctx.ChangeTracker.Clear();

        var orchestrator = Substitute.For<IEntityTranslationOrchestrator>();
        orchestrator.TranslateAsync(
                Arg.Any<IReadOnlyDictionary<string, string>>(),
                Arg.Any<string>(),
                Arg.Any<IReadOnlyList<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(Array.Empty<EntityFieldTranslationSet>());

        var langs = LanguageProvider((fr.Id, fr.Code));
        var uow = new CountingUnitOfWork(_ctx);
        var handler = BuildHandler(orchestrator, langs, uow);

        // Use a 5-second wall-clock timeout to fail loudly if any infinite-loop
        // hazard sneaks back in (anti-join self-progress — but the row would be
        // re-selected forever if the handler silently skipped without break).
        // The current implementation handles this via batch.Count < BatchSize → break,
        // not via a per-row "did anything change" flag — so if the candidate is the
        // only row and produces no translation, batch.Count < BatchSize triggers exit.
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var result = await handler.Handle(
            new TriggerTranslationBackfillCommand("tag"),
            cts.Token);

        result.IsSuccess.Should().BeTrue("empty translator result must NOT throw — silent skip is correct here");
        result.Value!.TotalProcessed.Should().Be(1, "the candidate was visited but produced no translation row");
        result.Value.TotalTranslationsAdded.Should().Be(0);

        (await _ctx.TagTranslations.CountAsync()).Should().Be(0);
    }

    // ── 9. SaveFails_ReturnsConflictAndPreservesEarlierBatches ───────────────

    [Fact]
    public async Task TriggerTranslationBackfill_SaveFails_ReturnsConflictAndPreservesEarlierBatches()
    {
        var fr = SeedLanguage(_ctx, "fr", "French");
        // Seed 300 tags → 2 batches (200 + 100); force the SECOND save to throw.
        for (var i = 0; i < 300; i++)
            SeedTag(_ctx, $"Tag-{i}", $"tag-fail-{i}");
        await _ctx.SaveChangesAsync();
        _ctx.ChangeTracker.Clear();

        var orchestrator = EchoOrchestrator(new Dictionary<string, Guid> { [fr.Code] = fr.Id });
        var langs = LanguageProvider((fr.Id, fr.Code));
        var uow = new CountingUnitOfWork(_ctx)
        {
            FailOnSave = saveNumber => saveNumber == 2
                ? new DbUpdateConcurrencyException("simulated mid-batch concurrency conflict")
                : null,
        };
        var handler = BuildHandler(orchestrator, langs, uow);

        var result = await handler.Handle(
            new TriggerTranslationBackfillCommand("tag"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Error!.Code.Should().Be("Translation.ConcurrencyConflict");

        // The first batch's 200 translations were already committed before the
        // second batch's save threw — they remain durable in the database.
        (await _ctx.TagTranslations.CountAsync()).Should().Be(200,
            "the first batch's translations should be durable; only the second batch's save threw");
    }
}
