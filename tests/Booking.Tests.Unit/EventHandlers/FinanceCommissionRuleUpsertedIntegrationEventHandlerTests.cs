using Booking.Application.Interfaces;
using Booking.Domain.Entities;
using Booking.Domain.Repositories;
using Booking.Infrastructure.EventHandlers;
using Finance.Contracts.IntegrationEvents;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Booking.Tests.Unit.EventHandlers;

public sealed class FinanceCommissionRuleUpsertedIntegrationEventHandlerTests
{
    private static IntegrationEventNotification<CommissionRuleUpsertedIntegrationEvent>
        Notification(Guid messageId, CommissionRuleUpsertedIntegrationEvent evt) =>
            new(messageId, evt);

    private static CommissionRuleUpsertedIntegrationEvent NewEvent(
        Guid ruleId, string tier = "Free", string currency = "JOD", decimal percentage = 12.5m, DateTime? occurredAt = null)
        => new(ruleId, tier, MinMonthlyRevenue: 0m, MaxMonthlyRevenue: null,
            Currency: currency, Percentage: percentage,
            OccurredAt: occurredAt ?? DateTime.UtcNow);

    [Fact]
    public async Task First_delivery_creates_snapshot()
    {
        var repo = Substitute.For<ICommissionSnapshotRepository>();
        var inbox = Substitute.For<IBookingInboxStore>();
        var uow = Substitute.For<IBookingUnitOfWork>();
        var handler = new FinanceCommissionRuleUpsertedIntegrationEventHandler(
            repo, inbox, uow, NullLogger<FinanceCommissionRuleUpsertedIntegrationEventHandler>.Instance);

        var ruleId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        inbox.HasBeenProcessedAsync(messageId, Arg.Any<CancellationToken>()).Returns(false);
        repo.GetByIdAsync(ruleId, Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns((CommissionSnapshot?)null);

        await handler.Handle(Notification(messageId, NewEvent(ruleId)), CancellationToken.None);

        await repo.Received(1).AddAsync(
            Arg.Is<CommissionSnapshot>(s => s.Id == ruleId && s.IsActive && s.Tier == "Free"),
            Arg.Any<CancellationToken>());
        inbox.Received(1).MarkAsProcessed(messageId);
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Idempotent_when_already_processed()
    {
        var repo = Substitute.For<ICommissionSnapshotRepository>();
        var inbox = Substitute.For<IBookingInboxStore>();
        var uow = Substitute.For<IBookingUnitOfWork>();
        var handler = new FinanceCommissionRuleUpsertedIntegrationEventHandler(
            repo, inbox, uow, NullLogger<FinanceCommissionRuleUpsertedIntegrationEventHandler>.Instance);

        var ruleId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        inbox.HasBeenProcessedAsync(messageId, Arg.Any<CancellationToken>()).Returns(true);

        await handler.Handle(Notification(messageId, NewEvent(ruleId)), CancellationToken.None);

        await repo.DidNotReceiveWithAnyArgs().AddAsync(Arg.Any<CommissionSnapshot>(), Arg.Any<CancellationToken>());
        repo.DidNotReceiveWithAnyArgs().Update(Arg.Any<CommissionSnapshot>());
        inbox.DidNotReceiveWithAnyArgs().MarkAsProcessed(default);
        await uow.DidNotReceiveWithAnyArgs().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Existing_snapshot_is_updated_in_place()
    {
        var repo = Substitute.For<ICommissionSnapshotRepository>();
        var inbox = Substitute.For<IBookingInboxStore>();
        var uow = Substitute.For<IBookingUnitOfWork>();
        var handler = new FinanceCommissionRuleUpsertedIntegrationEventHandler(
            repo, inbox, uow, NullLogger<FinanceCommissionRuleUpsertedIntegrationEventHandler>.Instance);

        var ruleId = Guid.NewGuid();
        var existing = CommissionSnapshot.Create(ruleId, "Free", "JOD", 10m, DateTime.UtcNow.AddDays(-1));
        repo.GetByIdAsync(ruleId, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(existing);

        var messageId = Guid.NewGuid();
        var evt = NewEvent(ruleId, tier: "Premium", currency: "USD", percentage: 22m, occurredAt: DateTime.UtcNow);
        await handler.Handle(Notification(messageId, evt), CancellationToken.None);

        existing.Tier.Should().Be("Premium");
        existing.Currency.Should().Be("USD");
        existing.Percentage.Should().Be(22m);
        existing.IsActive.Should().BeTrue();
        repo.Received(1).Update(existing);
        inbox.Received(1).MarkAsProcessed(messageId);
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
