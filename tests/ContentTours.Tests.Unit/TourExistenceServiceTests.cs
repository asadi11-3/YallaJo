using ContentTours.Contracts.Tours;
using ContentTours.Infrastructure.Persistence;
using ContentTours.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ContentTours.Tests.Unit;

/// <summary>
/// Cross-module read-only existence-service contract tests.  These pin the
/// three observable states (Active / NotFound / Deleted) so consumers like
/// ContentBlogs.LinkBlogToursCommandHandler can rely on a stable mapping
/// from TourId → status.
/// </summary>
public sealed class TourExistenceServiceTests
{
    [Fact]
    public async Task TourExistenceService_ReturnsActive_WhenTourExists()
    {
        await using var db = NewDb();
        var tour = TestTourFactory.CreateDraft();
        db.Tours.Add(tour);
        await db.SaveChangesAsync();

        var sut = new TourExistenceService(db);

        var status = await sut.GetStatusAsync(tour.Id);

        status.Should().Be(TourExistenceStatus.Active);
    }

    [Fact]
    public async Task TourExistenceService_ReturnsNotFound_WhenMissing()
    {
        await using var db = NewDb();

        var sut = new TourExistenceService(db);

        var status = await sut.GetStatusAsync(Guid.NewGuid());

        status.Should().Be(TourExistenceStatus.NotFound);
    }

    [Fact]
    public async Task TourExistenceService_ReturnsDeleted_WhenSoftDeleted()
    {
        await using var db = NewDb();
        var tour = TestTourFactory.CreateDraft();
        // SoftDelete is on AuditableEntity; reflection is required because the
        // setter is protected.  Mirrors the in-memory test approach used by
        // BlogLifecycleCommandHandlerTests.
        var isDeleted = typeof(YallaJo.SharedKernel.Domain.Entities.AuditableEntity)
            .GetProperty(
                nameof(YallaJo.SharedKernel.Domain.Entities.AuditableEntity.IsDeleted),
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic);
        isDeleted!.SetValue(tour, true);

        db.Tours.Add(tour);
        await db.SaveChangesAsync();

        var sut = new TourExistenceService(db);

        var status = await sut.GetStatusAsync(tour.Id);

        status.Should().Be(TourExistenceStatus.Deleted,
            "IgnoreQueryFilters in the service must reveal soft-deleted rows " +
            "and report them as Deleted, not NotFound");
    }

    private static ContentToursDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<ContentToursDbContext>()
            .UseInMemoryDatabase($"content-tours-existence-{Guid.NewGuid():N}")
            .Options;
        return new ContentToursDbContext(options);
    }
}
