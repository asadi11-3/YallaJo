using System.Linq.Expressions;
using ContentCore.Application.Commands.Category.DeleteCategory;
using ContentCore.Application.Commands.Category.RestoreCategory;
using ContentCore.Domain.Entities;
using ContentCore.Domain.Events;
using ContentCore.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Tests.Unit;

// ── Shared builder ─────────────────────────────────────────────────────────

file static class CategoryHandlerBuilder
{
    private const string DefaultLang = "en";

    /// <summary>Returns a fresh non-deleted Category with no pending domain events.</summary>
    internal static Category LiveCategory(string name = "Culture", string slug = "culture")
    {
        var c = Category.Create(name, slug, DefaultLang);
        c.ClearDomainEvents();
        return c;
    }

    /// <summary>Returns a soft-deleted Category with no pending domain events.</summary>
    internal static Category SoftDeletedCategory(string name = "Culture", string slug = "culture")
    {
        var c = Category.Create(name, slug, DefaultLang);
        c.SoftDelete();
        c.ClearDomainEvents();
        return c;
    }

    internal static DeleteCategoryCommandHandler BuildDeleteHandler(
        ICategoryRepository categoryRepository,
        IContentCoreUnitOfWork? unitOfWork = null,
        HybridCache? cache = null) =>
        new(categoryRepository,
            unitOfWork ?? OwnershipAuthFixture.NoOpUnitOfWork(),
            cache ?? OwnershipAuthFixture.NoOpCache(),
            Substitute.For<ILogger<DeleteCategoryCommandHandler>>());

    internal static RestoreCategoryCommandHandler BuildRestoreHandler(
        ICategoryRepository categoryRepository,
        IContentCoreUnitOfWork? unitOfWork = null,
        HybridCache? cache = null) =>
        new(categoryRepository,
            unitOfWork ?? OwnershipAuthFixture.NoOpUnitOfWork(),
            cache ?? OwnershipAuthFixture.NoOpCache(),
            Substitute.For<ILogger<RestoreCategoryCommandHandler>>());
}

// ── DeleteCategoryCommandHandler — domain-event regression ─────────────────

/// <summary>
/// T4 handler-level regression for CONTENTCORE-STD-P1-001.
/// Proves DeleteCategoryCommandHandler delegates domain-event emission to
/// <see cref="Category.SoftDelete()"/> and never raises a duplicate event.
/// </summary>
public sealed class DeleteCategoryCommandHandlerEventTests
{
    [Fact]
    public async Task Handle_ShouldRaiseExactlyOneCategoryDeletedDomainEvent_WhenDeleteSucceeds()
    {
        var category = CategoryHandlerBuilder.LiveCategory();

        var repository = Substitute.For<ICategoryRepository>();
        repository.GetByIdAsync(category.Id, Arg.Any<CancellationToken>(), false).Returns(category);
        // AnyAsync: no child categories → allow delete
        repository.AnyAsync(Arg.Any<Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);
        var unitOfWork = Substitute.For<IContentCoreUnitOfWork>();
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = CategoryHandlerBuilder.BuildDeleteHandler(repository, unitOfWork);
        var result = await handler.Handle(new DeleteCategoryCommand(category.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Exactly one event — handler calls SoftDelete() which encapsulates the event.
        // DomainEventAssertions.ShouldContainDomainEvent uses SingleOrDefault so it
        // fails if there are zero OR more than one events.
        var evt = category.DomainEvents.OfType<CategoryDeletedDomainEvent>()
            .Should().ContainSingle("handler must delegate to SoftDelete() which raises exactly one event").Subject;
        evt.CategoryId.Should().Be(category.Id);
        evt.Slug.Should().Be(category.Slug);
        category.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldNotRaiseDomainEvent_WhenCategoryNotFound()
    {
        var repository = Substitute.For<ICategoryRepository>();
        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), false)
            .Returns((Category?)null);
        var unitOfWork = Substitute.For<IContentCoreUnitOfWork>();

        var handler = CategoryHandlerBuilder.BuildDeleteHandler(repository, unitOfWork);
        var result = await handler.Handle(new DeleteCategoryCommand(Guid.NewGuid()), CancellationToken.None);

        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().Contain(e => e.Code == "Category.NotFound");
        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_ShouldNotRaiseDomainEvent_WhenCategoryHasChildren()
    {
        var category = CategoryHandlerBuilder.LiveCategory();

        var repository = Substitute.For<ICategoryRepository>();
        repository.GetByIdAsync(category.Id, Arg.Any<CancellationToken>(), false).Returns(category);
        // AnyAsync: has child categories → block delete
        repository.AnyAsync(Arg.Any<Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);
        var unitOfWork = Substitute.For<IContentCoreUnitOfWork>();

        var handler = CategoryHandlerBuilder.BuildDeleteHandler(repository, unitOfWork);
        var result = await handler.Handle(new DeleteCategoryCommand(category.Id), CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().Contain(e => e.Code == "Category.HasChildren");
        category.DomainEvents.OfType<CategoryDeletedDomainEvent>()
            .Should().BeEmpty("SoftDelete must NOT be called when the category still has children");
        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }
}

// ── RestoreCategoryCommandHandler — domain-event regression ────────────────

/// <summary>
/// T4 handler-level regression for CONTENTCORE-STD-P1-001.
/// Proves RestoreCategoryCommandHandler delegates domain-event emission to
/// <see cref="Category.Restore()"/> and never raises a duplicate event.
/// </summary>
public sealed class RestoreCategoryCommandHandlerEventTests
{
    [Fact]
    public async Task Handle_ShouldRaiseExactlyOneCategoryRestoredDomainEvent_WhenRestoreSucceeds()
    {
        var category = CategoryHandlerBuilder.SoftDeletedCategory();

        var repository = Substitute.For<ICategoryRepository>();
        repository.GetByIdIncludingDeletedAsync(category.Id, Arg.Any<CancellationToken>()).Returns(category);
        var unitOfWork = Substitute.For<IContentCoreUnitOfWork>();
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = CategoryHandlerBuilder.BuildRestoreHandler(repository, unitOfWork);
        var result = await handler.Handle(new RestoreCategoryCommand(category.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Exactly one event — handler calls Restore() which encapsulates the event.
        var evt = category.DomainEvents.OfType<CategoryRestoredDomainEvent>()
            .Should().ContainSingle("handler must delegate to Restore() which raises exactly one event").Subject;
        evt.CategoryId.Should().Be(category.Id);
        evt.Slug.Should().Be(category.Slug);
        category.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldReturnInvalid_WhenCategoryIsAlreadyLive()
    {
        // Handler pre-checks `if (!category.IsDeleted)` before calling Restore(),
        // so the entity guard is never even reached on this path.
        var category = CategoryHandlerBuilder.LiveCategory();

        var repository = Substitute.For<ICategoryRepository>();
        repository.GetByIdIncludingDeletedAsync(category.Id, Arg.Any<CancellationToken>()).Returns(category);
        var unitOfWork = Substitute.For<IContentCoreUnitOfWork>();

        var handler = CategoryHandlerBuilder.BuildRestoreHandler(repository, unitOfWork);
        var result = await handler.Handle(new RestoreCategoryCommand(category.Id), CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().Contain(e => e.Code == "Category.NotDeleted");
        category.DomainEvents.OfType<CategoryRestoredDomainEvent>()
            .Should().BeEmpty("Restore must NOT be called when the category is already live");
        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenCategoryDoesNotExist()
    {
        var repository = Substitute.For<ICategoryRepository>();
        repository.GetByIdIncludingDeletedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Category?)null);
        var unitOfWork = Substitute.For<IContentCoreUnitOfWork>();

        var handler = CategoryHandlerBuilder.BuildRestoreHandler(repository, unitOfWork);
        var result = await handler.Handle(new RestoreCategoryCommand(Guid.NewGuid()), CancellationToken.None);

        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().Contain(e => e.Code == "Category.NotFound");
        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }
}
