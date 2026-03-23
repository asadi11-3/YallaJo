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
    public void Create_ShouldRaiseCategoryCreatedDomainEvent_WhenArgumentsAreValid()
    {
        var category = Category.Create("Nature", "nature", DefaultSourceLanguageCode);

        var domainEvent = DomainEventAssertions.ShouldContainDomainEvent<CategoryCreatedDomainEvent>(category);
        domainEvent.CategoryId.Should().Be(category.Id);
        domainEvent.Name.Should().Be("Nature");
        domainEvent.SourceLanguageCode.Should().Be(DefaultSourceLanguageCode);
    }

    [Fact]
    public void Update_ShouldChangeProperties_WhenArgumentsAreValid()
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
}
