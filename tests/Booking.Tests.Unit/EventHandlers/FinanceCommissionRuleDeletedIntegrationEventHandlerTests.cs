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

public sealed class FinanceCommissionRuleDeletedIntegrationEventHandlerTests
{
    private static IntegrationEventNotification<CommissionRuleDeletedIntegrationEvent>
        Notification(Guid messageId, Guid ruleId) =>
            new(messageId, new CommissionRuleDeletedIntegrationEvent(ruleId, DateTime.UtcNow));

    [Fact]
    public async Task Existing_active_snapshot_is_deactivated()
    {
        var repo = Substitute.For<ICommissionSnapshotRepository>();
        var inbox = Substitute.For<IBookingInboxStore>();
        var uow = Substitute.For<IBookingUnitOfWork>();
        var handler = new FinanceCommissionRuleDeletedIntegrationEventHandler(
            repo, inbox, uow, NullLogger<FinanceCommissionRuleDeletedIntegrationEventHandler>.Instance);

        var ruleId = Guid.NewGuid();
        var existing = CommissionSnapshot.Create(ruleId, "Free", "JOD", 10m, DateTime.UtcNow.AddDays(-1));
        existing.IsActive.Should().BeTrue();
        repo.GetByIdAsync(ruleId, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(existing);

        var messageId = Guid.NewGuid();
        await handler.Handle(Notification(messageId, ruleId), CancellationToken.None);

        existing.IsActive.Should().BeFalse();
        repo.Received(1).Update(existing);
        inbox.Received(1).MarkAsProcessed(messageId);
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Idempotent_when_already_processed()
    {
        var repo = Substitute.For<ICommissionSnapshotRepository>();
        var inbox = Substitute.For<IBookingInboxStore>();
        var uow = Substitute.For<IBookingUnitOfWork>();
        var handler = new FinanceCommissionRuleDeletedIntegrationEventHandler(
            repo, inbox, uow, NullLogger<FinanceCommissionRuleDeletedIntegrationEventHandler>.Instance);

        var messageId = Guid.NewGuid();
        inbox.HasBeenProcessedAsync(messageId, Arg.Any<CancellationToken>()).Returns(true);

        await handler.Handle(Notification(messageId, Guid.NewGuid()), CancellationToken.None);

        await repo.DidNotReceiveWithAnyArgs().GetByIdAsync(default, Arg.Any<CancellationToken>(), Arg.Any<bool>());
        inbox.DidNotReceiveWithAnyArgs().MarkAsProcessed(default);
        await uow.DidNotReceiveWithAnyArgs().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unknown_RuleId_is_still_marked_processed_to_avoid_replay_loop()
    {
        var repo = Substitute.For<ICommissionSnapshotRepository>();
        var inbox = Substitute.For<IBookingInboxStore>();
        var uow = Substitute.For<IBookingUnitOfWork>();
        var handler = new FinanceCommissionRuleDeletedIntegrationEventHandler(
            repo, inbox, uow, NullLogger<FinanceCommissionRuleDeletedIntegrationEventHandler>.Instance);

        repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns((CommissionSnapshot?)null);

        var messageId = Guid.NewGuid();
        await handler.Handle(Notification(messageId, Guid.NewGuid()), CancellationToken.None);

        inbox.Received(1).MarkAsProcessed(messageId);
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        repo.DidNotReceiveWithAnyArgs().Update(Arg.Any<CommissionSnapshot>());
    }
}
