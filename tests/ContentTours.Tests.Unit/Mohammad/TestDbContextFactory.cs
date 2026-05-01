using ContentTours.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ContentTours.Tests.Unit.Mohammad;

internal static class TestDbContextFactory
{
    /// <summary>
    /// Builds an isolated EF InMemory <see cref="ContentToursDbContext"/> for query-handler
    /// integration tests. Each call uses a unique database name so tests are independent.
    /// </summary>
    public static ContentToursDbContext NewInMemory()
    {
        var options = new DbContextOptionsBuilder<ContentToursDbContext>()
            .UseInMemoryDatabase(databaseName: $"tours-tests-{Guid.NewGuid():N}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ContentToursDbContext(options);
    }
}
