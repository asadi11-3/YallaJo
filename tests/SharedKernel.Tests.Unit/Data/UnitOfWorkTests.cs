using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Events;
using YallaJo.SharedKernel.Domain.Entities;
using YallaJo.SharedKernel.Domain.Event;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace SharedKernel.Tests.Unit;

public sealed class UnitOfWorkTests
{
    private sealed class TestDbContext(DbContextOptions<TestDbContext> options, IList<string> callOrder)
        : DbContext(options)
    {
        public DbSet<TestAggregate> Aggregates => Set<TestAggregate>();
        public DbSet<TestEntity> Entities => Set<TestEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestAggregate>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();
            });

            modelBuilder.Entity<TestEntity>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();
            });
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            callOrder.Add("SaveChangesAsync");
            return await base.SaveChangesAsync(cancellationToken);
        }
    }

    private sealed class TestAggregate : AuditableEntity<Guid>, IAggregateRoot
    {
        private TestAggregate() { }

        public TestAggregate(Guid id) => Id = id;
    }

    private sealed class TestEntity : BaseEntity<Guid>
    {
        private TestEntity() { }

        public TestEntity(Guid id) => Id = id;
    }

    private sealed record TestDomainEvent(string Value) : DomainEventBase;

    private static (TestDbContext Context, IDomainEventDispatcher Dispatcher, UnitOfWork<TestDbContext> UnitOfWork, IList<string> CallOrder)
        BuildHarness()
    {
        var callOrder = new List<string>();
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"uow-{Guid.NewGuid():N}", new InMemoryDatabaseRoot())
            .Options;

        var context = new TestDbContext(options, callOrder);
        var dispatcher = Substitute.For<IDomainEventDispatcher>();
        dispatcher
            .DispatchAsync(Arg.Any<IEnumerable<IDomainEvent>>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callOrder.Add("DispatchAsync");
                return Task.CompletedTask;
            });

        var unitOfWork = new UnitOfWork<TestDbContext>(context, dispatcher);
        return (context, dispatcher, unitOfWork, callOrder);
    }

    [Fact]
    public async Task SaveChangesAsync_DispatchesDomainEvents_BeforeSavingToDatabase()
    {
        var (context, dispatcher, unitOfWork, callOrder) = BuildHarness();
        var aggregate = new TestAggregate(Guid.NewGuid());
        var domainEvent = new TestDomainEvent("created");

        aggregate.AddDomainEvent(domainEvent);
        context.Aggregates.Add(aggregate);

        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        callOrder.Should().Equal("DispatchAsync", "SaveChangesAsync");
        await dispatcher.Received(1).DispatchAsync(
            Arg.Is<IEnumerable<IDomainEvent>>(events => events.Single().Equals(domainEvent)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveChangesAsync_ClearsDomainEvents_AfterDispatch()
    {
        var (context, dispatcher, unitOfWork, _) = BuildHarness();
        var aggregate = new TestAggregate(Guid.NewGuid());

        aggregate.AddDomainEvent(new TestDomainEvent("one"));
        aggregate.AddDomainEvent(new TestDomainEvent("two"));
        context.Aggregates.Add(aggregate);

        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        aggregate.DomainEvents.Should().BeEmpty();
        await dispatcher.Received(1).DispatchAsync(Arg.Any<IEnumerable<IDomainEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveChangesAsync_OnlyDispatches_FromIAggregateRootEntities()
    {
        var (context, dispatcher, unitOfWork, _) = BuildHarness();
        var entity = new TestEntity(Guid.NewGuid());

        entity.AddDomainEvent(new TestDomainEvent("ignored"));
        context.Entities.Add(entity);

        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        await dispatcher.DidNotReceive().DispatchAsync(Arg.Any<IEnumerable<IDomainEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveChangesAsync_DoesNotCallDispatcher_WhenNoEvents()
    {
        var (context, dispatcher, unitOfWork, _) = BuildHarness();
        var aggregate = new TestAggregate(Guid.NewGuid());

        context.Aggregates.Add(aggregate);

        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        await dispatcher.DidNotReceive().DispatchAsync(Arg.Any<IEnumerable<IDomainEvent>>(), Arg.Any<CancellationToken>());
    }
}
