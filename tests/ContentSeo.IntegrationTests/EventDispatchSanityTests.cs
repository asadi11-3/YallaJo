using ContentSeo.Application.Interfaces;
using ContentSeo.Domain.Repositories;
using ContentSeo.Domain.Entities;
using ContentSeo.Domain.Enums;
using ContentSeo.Domain.Events;
using ContentSeo.Infrastructure;
using ContentSeo.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YallaJo.SharedKernel.Application.Abstractions.Events;
using YallaJo.SharedKernel.Domain.Event;

namespace ContentSeo.IntegrationTests;

/// <summary>
/// PW-7 acceptance test (mirror of ContentBlogs equivalent).
///
/// Verifies the in-process plumbing that the outbox pipeline depends on:
///   1. <see cref="AddContentSeoInfrastructure"/> wires the full DI graph (UoW, dispatcher, MediatR, outbox processor).
///   2. <see cref="IContentSeoUnitOfWork"/> can be resolved from a request scope.
///   3. Persisting a domain-event-raising aggregate triggers
///      <see cref="IDomainEventDispatcher"/> exactly once with that event.
///
/// <para>
/// <b>Deviation from PW-7 brief (documented in <c>Mohammad-tasks.md</c>):</b>
/// the brief asks for <c>WebApplicationFactory&lt;Program&gt;</c>, but the
/// downstream assertion — <i>"OutboxMessages row created with logical name
/// content-seo.seo-metadata.created.v1"</i> — requires the
/// <c>SeoMetadataChangedIntegrationEvent</c> + matching
/// <c>INotificationHandler&lt;SeoMetadataCreatedDomainEvent&gt;</c> mapper which are
/// part of <b>T3 scope</b>, not pre-work. This test therefore validates the
/// plumbing that PW-1 + PW-4 + PW-5 + PW-6 add. The outbox-row assertion will be
/// appended in T3 once the integration event + mapper handler exist.
/// </para>
/// </summary>
public sealed class EventDispatchSanityTests
{
    [Fact]
    public async Task ContentSeoUnitOfWork_PersistingAggregate_DispatchesDomainEvent()
    {
        // Arrange — minimal host that exercises the same DI as production.
        await using var provider = BuildTestServices();

        await using var scope = provider.CreateAsyncScope();
        var sut = scope.ServiceProvider.GetRequiredService<IContentSeoUnitOfWork>();
        var context = scope.ServiceProvider.GetRequiredService<ContentSeoDbContext>();
        var capture = scope.ServiceProvider.GetRequiredService<DispatchedEventCapture>();

        var seo = SeoMetadata.Create(
            entityType: SeoEntityType.Blog,
            entityId: Guid.CreateVersion7(),
            metaTitle: "sanity title");

        var domainEvent = new SeoMetadataCreatedDomainEvent(
            SeoMetadataId: seo.Id,
            EntityType: seo.EntityType,
            EntityId: seo.EntityId);

        // AddDomainEvent is public on BaseEntity — call directly.
        seo.AddDomainEvent(domainEvent);

        context.SeoMetadata.Add(seo);

        // Act
        var rows = await sut.SaveChangesAsync(CancellationToken.None);

        // Assert
        rows.Should().BeGreaterThan(0);
        capture.Dispatched.Should().ContainSingle(e => e is SeoMetadataCreatedDomainEvent);
        seo.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ContentSeoInfrastructure_DependencyInjection_ResolvesAllPreWorkServices()
    {
        // Arrange / Act
        using var provider = BuildTestServices();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        // Assert — every pre-work seam wires.
        sp.GetService<IContentSeoUnitOfWork>().Should().NotBeNull("PW-1");
        sp.GetService<ISeoMetadataRepository>().Should().NotBeNull("PW-5");
        sp.GetService<IRedirectRepository>().Should().NotBeNull("PW-5");
        sp.GetService<IFaqItemRepository>().Should().NotBeNull("PW-5");
        sp.GetService<ISitemapEntryRepository>().Should().NotBeNull("PW-5");
        sp.GetService<IWeatherCacheRepository>().Should().NotBeNull("PW-5");

        var catalogs = sp.GetServices<YallaJo.SharedKernel.Application.Authorization.IPermissionCatalog>();
        catalogs.Should().Contain(c => c.ModuleName == "ContentSeo", "PW-6");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ServiceProvider BuildTestServices()
    {
        var services = new ServiceCollection();

        // Provide a fake connection string so AddContentSeoInfrastructure does not throw,
        // then replace the SQL Server DbContext with an InMemory store after the fact.
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=ignored;Database=test;",
            })
            .Build();

        services.AddLogging();
        services.AddContentSeoInfrastructure(config);

        // Replace ContentSeoDbContext with InMemory for the test.
        var dbContextDescriptor = services.Single(
            d => d.ServiceType == typeof(DbContextOptions<ContentSeoDbContext>));
        services.Remove(dbContextDescriptor);
        services.AddDbContext<ContentSeoDbContext>(o =>
            o.UseInMemoryDatabase($"contentseo-sanity-{Guid.NewGuid()}"));

        // Replace the production IDomainEventDispatcher (registered by SharedKernel)
        // with a capturing decorator so we can assert dispatch happened.
        var dispatcherDescriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IDomainEventDispatcher));
        if (dispatcherDescriptor is not null)
        {
            services.Remove(dispatcherDescriptor);
        }

        services.AddSingleton<DispatchedEventCapture>();
        services.AddScoped<IDomainEventDispatcher, CapturingDomainEventDispatcher>();

        return services.BuildServiceProvider();
    }

}

internal sealed class DispatchedEventCapture
{
    public List<IDomainEvent> Dispatched { get; } = [];
}

internal sealed class CapturingDomainEventDispatcher(DispatchedEventCapture capture)
    : IDomainEventDispatcher
{
    public Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken ct = default)
    {
        capture.Dispatched.AddRange(domainEvents);
        return Task.CompletedTask;
    }
}
