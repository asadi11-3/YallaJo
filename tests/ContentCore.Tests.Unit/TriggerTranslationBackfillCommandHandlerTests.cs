using System.Linq.Expressions;
using ContentCore.Application.Commands.Translation.TriggerTranslationBackfill;
using ContentCore.Domain.Entities;
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
    /// </summary>
    internal static TriggerTranslationBackfillCommandHandler Build(
        ITagRepository? tagRepository = null,
        ISpecializationRepository? specializationRepository = null,
        IContentCoreUnitOfWork? unitOfWork = null,
        IEntityTranslationOrchestrator? orchestrator = null) =>
        new(
            tagRepository      ?? Substitute.For<ITagRepository>(),
            specializationRepository ?? Substitute.For<ISpecializationRepository>(),
            unitOfWork         ?? OwnershipAuthFixture.NoOpUnitOfWork(),
            orchestrator       ?? NoOpOrchestrator(),
            Substitute.For<ILogger<TriggerTranslationBackfillCommandHandler>>());

    /// <summary>
    /// Orchestrator that always returns an empty translation set so the handler
    /// iterates, produces zero new translations (skipped++) and falls straight
    /// through to SaveChangesAsync — which is the seam under test.
    /// </summary>
    internal static IEntityTranslationOrchestrator NoOpOrchestrator()
    {
        var o = Substitute.For<IEntityTranslationOrchestrator>();
        o.TranslateToAllActiveLanguagesAsync(
                Arg.Any<IReadOnlyDictionary<string, string>>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(Array.Empty<EntityFieldTranslationSet>());
        return o;
    }

    // ── Entity helpers ───────────────────────────────────────────────────────

    internal static Tag OneTag() =>
        Tag.Create("Adventure", "adventure", "en");

    internal static Specialization OneSpecialization() =>
        Specialization.Create("Guide", null, null, "en");

    // ── Repository helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Configures the tag repository to return the given list for any
    /// GetAllAsync call (positional args matched via Arg.Any).
    /// </summary>
    internal static ITagRepository TagRepoReturning(List<Tag> tags)
    {
        var repo = Substitute.For<ITagRepository>();
        repo.GetAllAsync(
                Arg.Any<Expression<Func<Tag, bool>>>(),
                Arg.Any<Func<IQueryable<Tag>, IQueryable<Tag>>>(),
                Arg.Any<Func<IQueryable<Tag>, IOrderedQueryable<Tag>>>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(tags);
        return repo;
    }

    internal static ISpecializationRepository SpecRepoReturning(List<Specialization> specs)
    {
        var repo = Substitute.For<ISpecializationRepository>();
        repo.GetAllAsync(
                Arg.Any<Expression<Func<Specialization, bool>>>(),
                Arg.Any<Func<IQueryable<Specialization>, IQueryable<Specialization>>>(),
                Arg.Any<Func<IQueryable<Specialization>, IOrderedQueryable<Specialization>>>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(specs);
        return repo;
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

// ── Concurrency conflict tests (CONTENTCORE-STD-P2-001) ───────────────────

/// <summary>
/// Regression tests for the DbUpdateConcurrencyException handling added to
/// TriggerTranslationBackfillCommandHandler in CONTENTCORE-STD-P2-001.
///
/// Both backfill paths (tag and specialization) call a single
/// unitOfWork.SaveChangesAsync at the end of the iteration loop.
/// When that call throws DbUpdateConcurrencyException the handler must:
///   - catch the exception (not let it escape),
///   - return Outcome.Conflict,
///   - include Error.Code == "Translation.ConcurrencyConflict".
/// </summary>
public sealed class TriggerTranslationBackfillConcurrencyTests
{
    // ── tag path ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenTagSaveChangesThrowsDbUpdateConcurrencyException()
    {
        var handler = TranslationBackfillHandlerBuilder.Build(
            tagRepository: TranslationBackfillHandlerBuilder.TagRepoReturning(
                [TranslationBackfillHandlerBuilder.OneTag()]),
            unitOfWork: TranslationBackfillHandlerBuilder.ConcurrentUnitOfWork());

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
    public async Task Handle_ShouldReturnConflict_WhenTagSaveChangesThrows_EvenWhenListIsEmpty()
    {
        // SaveChangesAsync is called unconditionally after the loop,
        // even when there are no entities to process.
        var handler = TranslationBackfillHandlerBuilder.Build(
            tagRepository: TranslationBackfillHandlerBuilder.TagRepoReturning([]),
            unitOfWork: TranslationBackfillHandlerBuilder.ConcurrentUnitOfWork());

        var result = await handler.Handle(
            new TriggerTranslationBackfillCommand("tag"),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Conflict);
        result.Error!.Code.Should().Be("Translation.ConcurrencyConflict");
    }

    // ── specialization path ───────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenSpecializationSaveChangesThrowsDbUpdateConcurrencyException()
    {
        var handler = TranslationBackfillHandlerBuilder.Build(
            specializationRepository: TranslationBackfillHandlerBuilder.SpecRepoReturning(
                [TranslationBackfillHandlerBuilder.OneSpecialization()]),
            unitOfWork: TranslationBackfillHandlerBuilder.ConcurrentUnitOfWork());

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
    public async Task Handle_ShouldReturnConflict_WhenSpecializationSaveChangesThrows_EvenWhenListIsEmpty()
    {
        var handler = TranslationBackfillHandlerBuilder.Build(
            specializationRepository: TranslationBackfillHandlerBuilder.SpecRepoReturning([]),
            unitOfWork: TranslationBackfillHandlerBuilder.ConcurrentUnitOfWork());

        var result = await handler.Handle(
            new TriggerTranslationBackfillCommand("specialization"),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Conflict);
        result.Error!.Code.Should().Be("Translation.ConcurrencyConflict");
    }

    // ── exception does not escape ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldNotThrow_WhenSaveChangesThrowsDbUpdateConcurrencyException()
    {
        var handler = TranslationBackfillHandlerBuilder.Build(
            tagRepository: TranslationBackfillHandlerBuilder.TagRepoReturning(
                [TranslationBackfillHandlerBuilder.OneTag()]),
            unitOfWork: TranslationBackfillHandlerBuilder.ConcurrentUnitOfWork());

        // Must return a Result, never let DbUpdateConcurrencyException escape.
        var act = async () => await handler.Handle(
            new TriggerTranslationBackfillCommand("tag"),
            CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}

// ── Success-path sanity (CONTENTCORE-STD-P2-001 non-regression) ───────────

/// <summary>
/// Light sanity checks that the success path still works after the concurrency-
/// guard change. Intentionally minimal — exhaustive orchestration tests are out
/// of scope for this backfill phase.
/// </summary>
public sealed class TriggerTranslationBackfillSuccessTests
{
    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenTagBackfillCompletesWithoutConflict()
    {
        var tag = TranslationBackfillHandlerBuilder.OneTag();
        var uow = Substitute.For<IContentCoreUnitOfWork>();
        uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(0);

        var handler = TranslationBackfillHandlerBuilder.Build(
            tagRepository: TranslationBackfillHandlerBuilder.TagRepoReturning([tag]),
            unitOfWork: uow);

        var result = await handler.Handle(
            new TriggerTranslationBackfillCommand("tag"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Ok);
        result.Value.Should().NotBeNull();
        result.Value!.EntityKind.Should().Be("tag");
        result.Value.TotalProcessed.Should().Be(1);
        // Orchestrator returns empty sets → nothing added → entity is skipped
        result.Value.TotalTranslationsAdded.Should().Be(0);
        result.Value.TotalSkipped.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenSpecializationBackfillCompletesWithoutConflict()
    {
        var spec = TranslationBackfillHandlerBuilder.OneSpecialization();
        var uow = Substitute.For<IContentCoreUnitOfWork>();
        uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(0);

        var handler = TranslationBackfillHandlerBuilder.Build(
            specializationRepository: TranslationBackfillHandlerBuilder.SpecRepoReturning([spec]),
            unitOfWork: uow);

        var result = await handler.Handle(
            new TriggerTranslationBackfillCommand("specialization"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EntityKind.Should().Be("specialization");
        result.Value.TotalProcessed.Should().Be(1);
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

        var handler = TranslationBackfillHandlerBuilder.Build(
            tagRepository: TranslationBackfillHandlerBuilder.TagRepoReturning([]),
            unitOfWork: uow);

        var upper = await handler.Handle(
            new TriggerTranslationBackfillCommand("TAG"), CancellationToken.None);
        var mixed = await handler.Handle(
            new TriggerTranslationBackfillCommand("Tag"), CancellationToken.None);

        upper.IsSuccess.Should().BeTrue("EntityKind matching must be case-insensitive");
        mixed.IsSuccess.Should().BeTrue("EntityKind matching must be case-insensitive");
    }
}
