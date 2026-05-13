using ContentCore.Application.Commands.Translation.TriggerTranslationBackfill;
using ContentCore.Application.Interfaces;
using ContentCore.Domain.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Tests.Unit;

// ── Shared builder ─────────────────────────────────────────────────────────

file static class TranslationBackfillHandlerBuilder
{
    /// <summary>
    /// Builds the handler with sensible no-op defaults for dependencies that are
    /// not under test.  Individual tests override only the subs they care about.
    ///
    /// CONTENTCORE-FOLLOWUP-BACKFILL-001 — handler now takes
    /// <see cref="IActiveLanguageProvider"/> and <see cref="ITranslationBackfillStore"/>
    /// instead of the prior repository-based dependencies.
    /// </summary>
    internal static TriggerTranslationBackfillCommandHandler Build(
        IContentCoreUnitOfWork? unitOfWork = null,
        IEntityTranslationOrchestrator? orchestrator = null,
        IActiveLanguageProvider? activeLanguageProvider = null,
        ITranslationBackfillStore? backfillStore = null) =>
        new(
            unitOfWork             ?? OwnershipAuthFixture.NoOpUnitOfWork(),
            orchestrator           ?? NoOpOrchestrator(),
            activeLanguageProvider ?? NoOpLanguageProvider(),
            backfillStore          ?? NoOpBackfillStore(),
            Substitute.For<ILogger<TriggerTranslationBackfillCommandHandler>>());

    /// <summary>
    /// Orchestrator that always returns an empty translation set so the handler
    /// iterates, produces zero new translations, and falls straight through to
    /// the per-batch SaveChangesAsync seam.
    /// </summary>
    internal static IEntityTranslationOrchestrator NoOpOrchestrator()
    {
        var o = Substitute.For<IEntityTranslationOrchestrator>();
        o.TranslateAsync(
                Arg.Any<IReadOnlyDictionary<string, string>>(),
                Arg.Any<string>(),
                Arg.Any<IReadOnlyList<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(Array.Empty<EntityFieldTranslationSet>());
        o.TranslateToAllActiveLanguagesAsync(
                Arg.Any<IReadOnlyDictionary<string, string>>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(Array.Empty<EntityFieldTranslationSet>());
        return o;
    }

    /// <summary>
    /// Active-language provider that returns zero active languages — the
    /// anti-join then has nothing to filter against and yields zero
    /// candidates.  Use this for the success / concurrency / case-insensitivity
    /// tests that don't care about candidate iteration.
    /// </summary>
    internal static IActiveLanguageProvider NoOpLanguageProvider()
    {
        var p = Substitute.For<IActiveLanguageProvider>();
        p.GetActiveLanguagesAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ActiveLanguage>());
        return p;
    }

    /// <summary>
    /// Backfill store that returns empty candidate batches — the handler exits
    /// the while-loop on the first iteration without calling Add* or
    /// translator.  Use this for tests that exercise the inbox/concurrency
    /// surface, not the iteration surface.
    /// </summary>
    internal static ITranslationBackfillStore NoOpBackfillStore()
    {
        var s = Substitute.For<ITranslationBackfillStore>();
        s.FetchNextTagBackfillCandidatesAsync(
                Arg.Any<IReadOnlyList<Guid>>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TagBackfillCandidate>());
        s.FetchNextSpecializationBackfillCandidatesAsync(
                Arg.Any<IReadOnlyList<Guid>>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(Array.Empty<SpecializationBackfillCandidate>());
        s.GetExistingTagTranslationLanguageIdsAsync(
                Arg.Any<IReadOnlyList<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TranslationLanguagePair>());
        s.GetExistingSpecializationTranslationLanguageIdsAsync(
                Arg.Any<IReadOnlyList<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TranslationLanguagePair>());
        return s;
    }

    /// <summary>
    /// Backfill store that returns ONE Tag candidate on the first fetch, then
    /// empty.  Used to exercise the per-batch SaveChanges path with a non-zero
    /// batch.
    /// </summary>
    internal static ITranslationBackfillStore TagStoreWithSingleCandidate(Guid tagId)
    {
        var s = NoOpBackfillStore();
        s.FetchNextTagBackfillCandidatesAsync(
                Arg.Any<IReadOnlyList<Guid>>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(
                _ => new[] { new TagBackfillCandidate(tagId, "Adventure", "en") },
                _ => Array.Empty<TagBackfillCandidate>());
        return s;
    }

    /// <summary>
    /// Backfill store that returns ONE Specialization candidate on the first
    /// fetch, then empty.
    /// </summary>
    internal static ITranslationBackfillStore SpecStoreWithSingleCandidate(Guid specId)
    {
        var s = NoOpBackfillStore();
        s.FetchNextSpecializationBackfillCandidatesAsync(
                Arg.Any<IReadOnlyList<Guid>>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(
                _ => new[] { new SpecializationBackfillCandidate(specId, "Guide", null, "en") },
                _ => Array.Empty<SpecializationBackfillCandidate>());
        return s;
    }

    /// <summary>
    /// Active-language provider that returns ONE active language so the
    /// anti-join has a non-empty filter set and the handler reaches the
    /// per-candidate orchestrator/Add path.
    /// </summary>
    internal static IActiveLanguageProvider OneActiveLanguageProvider(Guid languageId, string code = "fr")
    {
        var p = Substitute.For<IActiveLanguageProvider>();
        p.GetActiveLanguagesAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { new ActiveLanguage(languageId, code) });
        return p;
    }

    /// <summary>Returns a unit of work whose SaveChangesAsync throws DbUpdateConcurrencyException.</summary>
    internal static IContentCoreUnitOfWork ConcurrentUnitOfWork()
    {
        var uow = Substitute.For<IContentCoreUnitOfWork>();
        uow.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<int>(new DbUpdateConcurrencyException()));
        return uow;
    }
}

// ── Concurrency conflict tests (CONTENTCORE-STD-P2-001 — preserved behavior) ─

/// <summary>
/// Regression tests for the DbUpdateConcurrencyException handling on the
/// TriggerTranslationBackfillCommandHandler save path.
///
/// <para>
/// Under CONTENTCORE-FOLLOWUP-BACKFILL-001 the handler now performs per-batch
/// SaveChangesAsync — but only when there is a non-empty candidate batch.
/// To force the save path we wire up a single-candidate store and a single
/// active language so the handler reaches the Add → SaveChanges seam.
/// </para>
///
/// <para>
/// On <see cref="DbUpdateConcurrencyException"/> the handler must:
/// catch the exception (not let it escape), return <c>Outcome.Conflict</c>,
/// and include <c>Error.Code == "Translation.ConcurrencyConflict"</c>.
/// </para>
/// </summary>
public sealed class TriggerTranslationBackfillConcurrencyTests
{
    // ── tag path ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenTagSaveChangesThrowsDbUpdateConcurrencyException()
    {
        var langId = Guid.NewGuid();
        var handler = TranslationBackfillHandlerBuilder.Build(
            unitOfWork:             TranslationBackfillHandlerBuilder.ConcurrentUnitOfWork(),
            activeLanguageProvider: TranslationBackfillHandlerBuilder.OneActiveLanguageProvider(langId),
            backfillStore:          TranslationBackfillHandlerBuilder.TagStoreWithSingleCandidate(Guid.NewGuid()));

        var result = await handler.Handle(
            new TriggerTranslationBackfillCommand("tag"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().ContainSingle();
        result.Error!.Code.Should().Be("Translation.ConcurrencyConflict");
        result.Error.Message.Should().Be("One or more records were modified by another user. Please retry.");
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenTagBatchIsEmpty_EvenIfUowWouldThrow()
    {
        // Under the new design, SaveChanges is called only when there is a
        // non-empty candidate batch.  An empty backlog therefore never reaches
        // the (would-be-throwing) save and the handler returns Success.
        var handler = TranslationBackfillHandlerBuilder.Build(
            unitOfWork: TranslationBackfillHandlerBuilder.ConcurrentUnitOfWork());

        var result = await handler.Handle(
            new TriggerTranslationBackfillCommand("tag"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(
            "with no candidates the handler exits before the per-batch SaveChanges and never sees the conflict");
        result.Value!.TotalProcessed.Should().Be(0);
        result.Value.TotalTranslationsAdded.Should().Be(0);
        result.Value.TotalSkipped.Should().Be(0);
    }

    // ── specialization path ───────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenSpecializationSaveChangesThrowsDbUpdateConcurrencyException()
    {
        var langId = Guid.NewGuid();
        var handler = TranslationBackfillHandlerBuilder.Build(
            unitOfWork:             TranslationBackfillHandlerBuilder.ConcurrentUnitOfWork(),
            activeLanguageProvider: TranslationBackfillHandlerBuilder.OneActiveLanguageProvider(langId),
            backfillStore:          TranslationBackfillHandlerBuilder.SpecStoreWithSingleCandidate(Guid.NewGuid()));

        var result = await handler.Handle(
            new TriggerTranslationBackfillCommand("specialization"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().ContainSingle();
        result.Error!.Code.Should().Be("Translation.ConcurrencyConflict");
        result.Error.Message.Should().Be("One or more records were modified by another user. Please retry.");
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenSpecializationBatchIsEmpty_EvenIfUowWouldThrow()
    {
        var handler = TranslationBackfillHandlerBuilder.Build(
            unitOfWork: TranslationBackfillHandlerBuilder.ConcurrentUnitOfWork());

        var result = await handler.Handle(
            new TriggerTranslationBackfillCommand("specialization"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(
            "with no candidates the handler exits before the per-batch SaveChanges and never sees the conflict");
        result.Value!.TotalProcessed.Should().Be(0);
        result.Value.TotalTranslationsAdded.Should().Be(0);
        result.Value.TotalSkipped.Should().Be(0);
    }

    // ── exception does not escape ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldNotThrow_WhenSaveChangesThrowsDbUpdateConcurrencyException()
    {
        var langId = Guid.NewGuid();
        var handler = TranslationBackfillHandlerBuilder.Build(
            unitOfWork:             TranslationBackfillHandlerBuilder.ConcurrentUnitOfWork(),
            activeLanguageProvider: TranslationBackfillHandlerBuilder.OneActiveLanguageProvider(langId),
            backfillStore:          TranslationBackfillHandlerBuilder.TagStoreWithSingleCandidate(Guid.NewGuid()));

        var act = async () => await handler.Handle(
            new TriggerTranslationBackfillCommand("tag"),
            CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}

// ── Success-path sanity (CONTENTCORE-STD-P2-001 non-regression) ───────────

/// <summary>
/// Light sanity checks that the success path still works after the
/// CONTENTCORE-FOLLOWUP-BACKFILL-001 redesign.  Reporting semantics shifted:
/// the anti-join filters fully-translated rows out at the SQL layer, so
/// <c>TotalSkipped</c> is now always <c>0</c> and <c>TotalProcessed</c> counts
/// candidates that were actually touched (not pre-filter row counts).
/// Comprehensive batching/anti-join behaviour is covered by
/// <c>TriggerTranslationBackfillBatchingTests</c>.
/// </summary>
public sealed class TriggerTranslationBackfillSuccessTests
{
    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenTagBackfillCompletesWithoutConflict()
    {
        // With no active languages the anti-join filter set is empty and the
        // store returns zero candidates — nothing to process, nothing to add.
        // The result record shape is preserved; only TotalProcessed/TotalSkipped
        // semantics shifted (both 0 here under the new design).
        var uow = Substitute.For<IContentCoreUnitOfWork>();
        uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(0);

        var handler = TranslationBackfillHandlerBuilder.Build(unitOfWork: uow);

        var result = await handler.Handle(
            new TriggerTranslationBackfillCommand("tag"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Ok);
        result.Value.Should().NotBeNull();
        result.Value!.EntityKind.Should().Be("tag");
        result.Value.TotalProcessed.Should().Be(0);
        result.Value.TotalTranslationsAdded.Should().Be(0);
        result.Value.TotalSkipped.Should().Be(0,
            "CONTENTCORE-FOLLOWUP-BACKFILL-001: anti-join filters fully-translated rows " +
            "at the SQL layer, so the in-handler 'skipped' counter is always 0");
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenSpecializationBackfillCompletesWithoutConflict()
    {
        var uow = Substitute.For<IContentCoreUnitOfWork>();
        uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(0);

        var handler = TranslationBackfillHandlerBuilder.Build(unitOfWork: uow);

        var result = await handler.Handle(
            new TriggerTranslationBackfillCommand("specialization"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EntityKind.Should().Be("specialization");
        result.Value.TotalProcessed.Should().Be(0);
        result.Value.TotalSkipped.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldReturnInvalid_WhenEntityKindIsUnknown()
    {
        var handler = TranslationBackfillHandlerBuilder.Build();

        var result = await handler.Handle(
            new TriggerTranslationBackfillCommand("unknown-kind"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Error!.Code.Should().Be("Backfill.InvalidKind");
    }

    [Fact]
    public async Task Handle_ShouldBeCaseInsensitive_ForEntityKind()
    {
        // Handler calls .ToLowerInvariant() before the switch — "TAG", "Tag", "tag" all route
        // to the same branch.
        var uow = Substitute.For<IContentCoreUnitOfWork>();
        uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(0);

        var handler = TranslationBackfillHandlerBuilder.Build(unitOfWork: uow);

        var upper = await handler.Handle(
            new TriggerTranslationBackfillCommand("TAG"), CancellationToken.None);
        var mixed = await handler.Handle(
            new TriggerTranslationBackfillCommand("Tag"), CancellationToken.None);

        upper.IsSuccess.Should().BeTrue("EntityKind matching must be case-insensitive");
        mixed.IsSuccess.Should().BeTrue("EntityKind matching must be case-insensitive");
    }
}
