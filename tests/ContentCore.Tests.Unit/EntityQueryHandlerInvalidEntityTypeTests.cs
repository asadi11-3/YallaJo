using ContentCore.Application.Queries.EntityCategory.GetEntityCategories;
using ContentCore.Application.Queries.EntityTag.GetEntityTags;
using ContentCore.Domain.Entities;
using ContentCore.Domain.Enums;
using ContentCore.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Tests.Unit;

// ── Builders ──────────────────────────────────────────────────────────────────

file static class CategoryQueryHandlerBuilder
{
    internal static GetEntityCategoriesQueryHandler Build(
        IEntityCategoryRepository? repo = null) =>
        new(
            repo ?? Substitute.For<IEntityCategoryRepository>(),
            Substitute.For<ILogger<GetEntityCategoriesQueryHandler>>());
}

file static class TagQueryHandlerBuilder
{
    internal static GetEntityTagsQueryHandler Build(
        IEntityTagRepository? repo = null) =>
        new(
            repo ?? Substitute.For<IEntityTagRepository>(),
            Substitute.For<ILogger<GetEntityTagsQueryHandler>>());
}

// ── GetEntityCategoriesQueryHandler — invalid EntityType (P2-004) ─────────────

/// <summary>
/// Regression tests for CONTENTCORE-STD-P2-004.
/// GetEntityCategoriesQueryHandler must return Outcome.Invalid for an unrecognised
/// EntityType string and must NOT call the repository or return Success(empty).
/// </summary>
public sealed class GetEntityCategoriesQueryHandlerInvalidEntityTypeTests
{
    // ── Core invalid-EntityType assertion ─────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldReturnInvalid_WhenEntityTypeIsUnrecognized()
    {
        var handler = CategoryQueryHandlerBuilder.Build();

        var result = await handler.Handle(
            new GetEntityCategoriesQuery(OwnershipAuthFixture.InvalidEntityTypeString, Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Invalid);
        // Must NOT return Success(empty) — Value is null on failure paths
        result.Value.Should().BeNull("invalid EntityType must never produce Success(empty)");
        result.Errors.Should().ContainSingle(e =>
            e.Code == "EntityCategory.InvalidEntityType" &&
            e.Message == "Invalid entity type.");
    }

    // ── Repository isolation ──────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldNotCallRepository_WhenEntityTypeIsInvalid()
    {
        var repo = Substitute.For<IEntityCategoryRepository>();
        var handler = CategoryQueryHandlerBuilder.Build(repo);

        await handler.Handle(
            new GetEntityCategoriesQuery(OwnershipAuthFixture.InvalidEntityTypeString, Guid.NewGuid()),
            CancellationToken.None);

        // Repository must be completely bypassed — no DB round-trip on a parse failure
        await repo.DidNotReceiveWithAnyArgs()
            .GetByEntityAsync(default, default, default);
    }

    // ── Success-path sanity (non-regression) ─────────────────────────────────

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenEntityTypeIsValid()
    {
        var repo = Substitute.For<IEntityCategoryRepository>();
        IReadOnlyList<EntityCategory> empty = new List<EntityCategory>();
        repo.GetByEntityAsync(
                Arg.Any<EntityType>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(empty);

        var handler = CategoryQueryHandlerBuilder.Build(repo);

        var result = await handler.Handle(
            new GetEntityCategoriesQuery(OwnershipAuthFixture.ValidEntityTypeString, Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Ok);
        result.Value.Should().NotBeNull().And.BeEmpty();
    }
}

// ── GetEntityTagsQueryHandler — invalid EntityType (P2-004b) ─────────────────

/// <summary>
/// Regression tests for CONTENTCORE-STD-P2-004b.
/// GetEntityTagsQueryHandler must return Outcome.Invalid for an unrecognised
/// EntityType string and must NOT call the repository or return Success(empty).
/// </summary>
public sealed class GetEntityTagsQueryHandlerInvalidEntityTypeTests
{
    // ── Core invalid-EntityType assertion ─────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldReturnInvalid_WhenEntityTypeIsUnrecognized()
    {
        var handler = TagQueryHandlerBuilder.Build();

        var result = await handler.Handle(
            new GetEntityTagsQuery(OwnershipAuthFixture.InvalidEntityTypeString, Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Value.Should().BeNull("invalid EntityType must never produce Success(empty)");
        result.Errors.Should().ContainSingle(e =>
            e.Code == "EntityTag.InvalidEntityType" &&
            e.Message == "Invalid entity type.");
    }

    // ── Repository isolation ──────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldNotCallRepository_WhenEntityTypeIsInvalid()
    {
        var repo = Substitute.For<IEntityTagRepository>();
        var handler = TagQueryHandlerBuilder.Build(repo);

        await handler.Handle(
            new GetEntityTagsQuery(OwnershipAuthFixture.InvalidEntityTypeString, Guid.NewGuid()),
            CancellationToken.None);

        await repo.DidNotReceiveWithAnyArgs()
            .GetByEntityAsync(default, default, default);
    }

    // ── Success-path sanity (non-regression) ─────────────────────────────────

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenEntityTypeIsValid()
    {
        var repo = Substitute.For<IEntityTagRepository>();
        IReadOnlyList<EntityTag> empty = new List<EntityTag>();
        repo.GetByEntityAsync(
                Arg.Any<EntityType>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(empty);

        var handler = TagQueryHandlerBuilder.Build(repo);

        var result = await handler.Handle(
            new GetEntityTagsQuery(OwnershipAuthFixture.ValidEntityTypeString, Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Ok);
        result.Value.Should().NotBeNull().And.BeEmpty();
    }
}
