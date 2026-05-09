using ContentCore.Domain.Entities;
using ContentCore.Domain.Events;
using FluentAssertions;
using YallaJo.Tests.Shared;

namespace ContentCore.Tests.Unit;

public sealed class CategoryTests : DomainTestBase
{
    [Fact]
    public void Create_ShouldCreateCategory_WhenArgumentsAreValid()
    {
        var category = Category.Create("Adventure", RandomSlug(), DefaultSourceLanguageCode);

        category.Name.Should().Be("Adventure");
        category.Slug.Should().NotBeNullOrWhiteSpace();
        category.IsActive.Should().BeTrue();
        category.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void CreateShouldRaiseCategoryCreatedDomainEventWhenArgumentsAreValid()
    {
        var category = Category.Create("Nature", "nature", DefaultSourceLanguageCode);

        var domainEvent = DomainEventAssertions.ShouldContainDomainEvent<CategoryCreatedDomainEvent>(category);
        domainEvent.CategoryId.Should().Be(category.Id);
        domainEvent.Name.Should().Be("Nature");
        domainEvent.SourceLanguageCode.Should().Be(DefaultSourceLanguageCode);
    }

    [Fact]
    public void UpdateShouldChangePropertiesWhenArgumentsAreValid()
    {
        var category = Category.Create("Old Name", "old-name", DefaultSourceLanguageCode);

        category.Update("New Name", "new-name", DefaultSourceLanguageCode);

        category.Name.Should().Be("New Name");
        category.Slug.Should().Be("new-name");
        category.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Update_ShouldRaiseCategoryUpdatedDomainEvent_WhenArgumentsAreValid()
    {
        var category = Category.Create("Old", "old", DefaultSourceLanguageCode);

        category.Update("Updated", "updated", DefaultSourceLanguageCode);

        var domainEvent = category.DomainEvents
            .OfType<CategoryUpdatedDomainEvent>()
            .Single();

        domainEvent.CategoryId.Should().Be(category.Id);
        domainEvent.Name.Should().Be("Updated");
        domainEvent.SourceLanguageCode.Should().Be(DefaultSourceLanguageCode);
    }

    [Fact]
    public void SoftDelete_ShouldSetIsDeletedTrue_WhenCategoryIsNotDeleted()
    {
        var category = Category.Create("Culture", "culture", DefaultSourceLanguageCode);

        category.SoftDelete();

        category.IsDeleted.Should().BeTrue();
        category.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenNameIsNull()
    {
        Action act = () => Category.Create(null!, "valid-slug", DefaultSourceLanguageCode);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    // ── SoftDelete — domain event regression (CONTENTCORE-STD-P1-001) ─────────

    [Fact]
    public void SoftDelete_ShouldRaiseCategoryDeletedDomainEvent()
    {
        var category = Category.Create("Culture", "culture", DefaultSourceLanguageCode);
        category.ClearDomainEvents(); // strip CategoryCreatedDomainEvent; focus on delete event

        category.SoftDelete();

        DomainEventAssertions.ShouldContainDomainEvent<CategoryDeletedDomainEvent>(category);
    }

    [Fact]
    public void SoftDelete_EventShouldContainCorrectCategoryIdAndSlug()
    {
        var slug = RandomSlug("delete");
        var category = Category.Create("Culture", slug, DefaultSourceLanguageCode);
        category.ClearDomainEvents();

        category.SoftDelete();

        var evt = DomainEventAssertions.ShouldContainDomainEvent<CategoryDeletedDomainEvent>(category);
        evt.CategoryId.Should().Be(category.Id);
        evt.Slug.Should().Be(slug);
    }

    [Fact]
    public void SoftDelete_IsIdempotent_SecondCallRaisesNoAdditionalEvent()
    {
        // Category.SoftDelete() guards with `if (IsDeleted) return;`
        // so the second call must be a no-op — exactly ONE event total.
        var category = Category.Create("Culture", "culture", DefaultSourceLanguageCode);
        category.ClearDomainEvents();

        category.SoftDelete();
        category.SoftDelete(); // idempotency guard fires — no second event

        category.DomainEvents
            .OfType<CategoryDeletedDomainEvent>()
            .Should().ContainSingle("the idempotency guard must prevent a second CategoryDeletedDomainEvent");
    }

    // ── Restore — domain event regression (CONTENTCORE-STD-P1-001) ────────────

    [Fact]
    public void Restore_ShouldRaiseCategoryRestoredDomainEvent_AfterSoftDelete()
    {
        var category = Category.Create("Culture", "culture", DefaultSourceLanguageCode);
        category.SoftDelete();
        category.ClearDomainEvents(); // isolate: only test the Restore event

        category.Restore();

        DomainEventAssertions.ShouldContainDomainEvent<CategoryRestoredDomainEvent>(category);
    }

    [Fact]
    public void Restore_EventShouldContainCorrectCategoryIdAndSlug_AndCategoryIsNoLongerDeleted()
    {
        var slug = RandomSlug("restore");
        var category = Category.Create("Culture", slug, DefaultSourceLanguageCode);
        category.SoftDelete();
        category.ClearDomainEvents();

        category.Restore();

        var evt = DomainEventAssertions.ShouldContainDomainEvent<CategoryRestoredDomainEvent>(category);
        evt.CategoryId.Should().Be(category.Id);
        evt.Slug.Should().Be(slug);
        category.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Restore_WhenCategoryIsNotDeleted_RaisesNoDomainEvent()
    {
        // Category.Restore() guards with `if (!IsDeleted) return;`
        // Calling Restore on a live (never-deleted) category must raise zero events.
        var category = Category.Create("Culture", "culture", DefaultSourceLanguageCode);
        category.ClearDomainEvents();

        category.Restore(); // guard fires — category is not deleted

        category.DomainEvents
            .OfType<CategoryRestoredDomainEvent>()
            .Should().BeEmpty("Restore on a live category must not raise CategoryRestoredDomainEvent");
        category.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Restore_IsIdempotent_SecondCallAfterRestoreRaisesNoAdditionalEvent()
    {
        // After first Restore: IsDeleted = false → second call hits the `!IsDeleted` guard.
        var category = Category.Create("Culture", "culture", DefaultSourceLanguageCode);
        category.SoftDelete();
        category.ClearDomainEvents();

        category.Restore();  // first: raises event, sets IsDeleted = false
        category.Restore();  // second: guard fires, no additional event

        category.DomainEvents
            .OfType<CategoryRestoredDomainEvent>()
            .Should().ContainSingle("the idempotency guard must prevent a second CategoryRestoredDomainEvent");
    }
}
