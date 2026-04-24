using Auth.Application.Interfaces;
using Auth.Contracts.IntegrationEvents;
using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using Auth.Infrastructure.EventHandlers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Tests.Unit;

/// <summary>
/// Phase 2C-3 — the outbox-dispatched
/// <see cref="ActivationEmailDispatchHandler"/> is the only place SMTP
/// runs on the activation path. These tests cover:
///   • inbox-level idempotency (already-processed messages skip silently),
///   • missing-token guard (mark inbox, stop retries),
///   • terminal-token guard (Consumed / Revoked → no email, inbox marked),
///   • expired-token guard (no email, inbox marked),
///   • already-Delivered guard (no duplicate email, inbox marked),
///   • happy path (MarkDelivered + inbox + single SaveChanges),
///   • SMTP failure (MarkDeliveryFailed, NO inbox mark, RETHROW).
/// </summary>
public sealed class ActivationEmailDispatchHandlerTests
{
    private readonly IActivationTokenRepository _tokenRepo  = Substitute.For<IActivationTokenRepository>();
    private readonly IAuthInboxStore            _inboxStore = Substitute.For<IAuthInboxStore>();
    private readonly IAuthUnitOfWork            _uow        = Substitute.For<IAuthUnitOfWork>();
    private readonly IEmailService              _email      = Substitute.For<IEmailService>();

    private ActivationEmailDispatchHandler CreateSut() =>
        new(_tokenRepo, _inboxStore, _uow, _email,
            NullLogger<ActivationEmailDispatchHandler>.Instance);

    private static ActivationTokenIssuedIntegrationEvent Event(Guid tokenId, Guid userId,
        string email = "invitee@example.com", string plain = "plain", string link = "https://link") =>
        new(
            TokenId:         tokenId,
            UserId:          userId,
            DeliveryAddress: email,
            PlainToken:      plain,
            ActivationLink:  link,
            ExpiresAt:       DateTime.UtcNow.AddDays(7));

    private static IntegrationEventNotification<ActivationTokenIssuedIntegrationEvent> Notification(
        Guid messageId, ActivationTokenIssuedIntegrationEvent ev) => new(messageId, ev);

    private void StubInboxProcessed(Guid messageId, bool processed) =>
        _inboxStore.HasBeenProcessedAsync(messageId, Arg.Any<CancellationToken>()).Returns(processed);

    private void StubToken(Guid tokenId, ActivationToken? token) =>
        _tokenRepo.GetByIdAsync(tokenId, Arg.Any<CancellationToken>(), Arg.Any<bool>()).Returns(token);

    [Fact]
    public async Task Handle_ShouldSkipSilently_WhenInboxAlreadyProcessed()
    {
        var messageId = Guid.NewGuid();
        StubInboxProcessed(messageId, true);

        var sut = CreateSut();

        await sut.Handle(Notification(messageId, Event(Guid.NewGuid(), Guid.NewGuid())), CancellationToken.None);

        await _tokenRepo.DidNotReceive().GetByIdAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>());
        await _email.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldMarkInboxProcessed_WhenTokenMissing()
    {
        var messageId = Guid.NewGuid();
        var tokenId = Guid.NewGuid();
        StubInboxProcessed(messageId, false);
        StubToken(tokenId, null);

        var sut = CreateSut();

        await sut.Handle(Notification(messageId, Event(tokenId, Guid.NewGuid())), CancellationToken.None);

        // Retries would keep failing forever against a missing row; mark
        // the inbox so the outbox processor stops re-delivering.
        _inboxStore.Received(1).MarkAsProcessed(messageId);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _email.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldSkipEmail_WhenTokenIsConsumed()
    {
        var messageId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var token = ActivationToken.Issue(userId, "h", "invitee@example.com", 60);
        token.MarkDelivered();
        token.Consume();

        StubInboxProcessed(messageId, false);
        StubToken(token.Id, token);

        var sut = CreateSut();

        await sut.Handle(Notification(messageId, Event(token.Id, userId)), CancellationToken.None);

        await _email.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        _inboxStore.Received(1).MarkAsProcessed(messageId);
    }

    [Fact]
    public async Task Handle_ShouldSkipEmail_WhenTokenIsRevoked()
    {
        var messageId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var token = ActivationToken.Issue(userId, "h", "invitee@example.com", 60);
        token.Supersede();

        StubInboxProcessed(messageId, false);
        StubToken(token.Id, token);

        var sut = CreateSut();

        await sut.Handle(Notification(messageId, Event(token.Id, userId)), CancellationToken.None);

        await _email.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        _inboxStore.Received(1).MarkAsProcessed(messageId);
    }

    [Fact]
    public async Task Handle_ShouldSkipEmail_WhenTokenExpired()
    {
        var messageId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var token = ActivationToken.Issue(userId, "h", "invitee@example.com", 60);
        typeof(ActivationToken).GetProperty(nameof(ActivationToken.ExpiresAt))!
            .GetSetMethod(nonPublic: true)!
            .Invoke(token, new object[] { DateTime.UtcNow.AddMinutes(-1) });

        StubInboxProcessed(messageId, false);
        StubToken(token.Id, token);

        var sut = CreateSut();

        await sut.Handle(Notification(messageId, Event(token.Id, userId)), CancellationToken.None);

        await _email.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        _inboxStore.Received(1).MarkAsProcessed(messageId);
        token.State.Should().Be(ActivationTokenState.Issued,
            "expiry is lazy — the dispatcher must not transition state for an expired row");
    }

    [Fact]
    public async Task Handle_ShouldSkipDuplicateSend_WhenTokenAlreadyDelivered()
    {
        var messageId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var token = ActivationToken.Issue(userId, "h", "invitee@example.com", 60);
        token.MarkDelivered();

        StubInboxProcessed(messageId, false);
        StubToken(token.Id, token);

        var sut = CreateSut();

        await sut.Handle(Notification(messageId, Event(token.Id, userId)), CancellationToken.None);

        // A prior dispatch already delivered the email; a retry must
        // not re-send and spam the user.
        await _email.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        _inboxStore.Received(1).MarkAsProcessed(messageId);
    }

    [Fact]
    public async Task Handle_OnHappyPath_ShouldSendEmail_MarkDelivered_AndMarkInbox()
    {
        var messageId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var token = ActivationToken.Issue(userId, "h", "invitee@example.com", 60);
        StubInboxProcessed(messageId, false);
        StubToken(token.Id, token);

        var ev = Event(token.Id, userId, email: "invitee@example.com",
            plain: "plain-xyz", link: "https://app/activate?t=plain-xyz");

        var sut = CreateSut();

        await sut.Handle(Notification(messageId, ev), CancellationToken.None);

        // Email was sent with the link from the event payload.
        await _email.Received(1).SendAsync(
            "invitee@example.com",
            Arg.Any<string>(),
            Arg.Is<string>(body => body.Contains("https://app/activate?t=plain-xyz", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());

        // Token advanced to Delivered only after SMTP success.
        token.State.Should().Be(ActivationTokenState.Delivered);
        token.DeliveryStatus.Should().Be(ActivationTokenDeliveryStatus.Sent);
        token.LastSentAt.Should().NotBeNull();

        _inboxStore.Received(1).MarkAsProcessed(messageId);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnSmtpFailure_ShouldMarkDeliveryFailed_NotMarkInbox_AndRethrow()
    {
        var messageId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var token = ActivationToken.Issue(userId, "h", "invitee@example.com", 60);
        StubInboxProcessed(messageId, false);
        StubToken(token.Id, token);

        _email.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => Task.FromException(new InvalidOperationException("SMTP DOWN")));

        var sut = CreateSut();

        var act = () => sut.Handle(Notification(messageId, Event(token.Id, userId)), CancellationToken.None);

        // Rethrow contract: the OutboxProcessor relies on the exception
        // to mark the outbox message failed and retry.
        await act.Should().ThrowAsync<InvalidOperationException>();

        // Non-terminal failure: token remains REDEEMABLE so outbox
        // retries can deliver the same valid link.
        token.State.Should().Be(ActivationTokenState.Issued,
            "Phase 2C-3: transient SMTP failures must NOT revoke the token");
        token.DeliveryStatus.Should().Be(ActivationTokenDeliveryStatus.Failed);
        token.LastSentAt.Should().NotBeNull();

        // Inbox must NOT be marked processed so the retry re-enters the
        // handler.
        _inboxStore.DidNotReceive().MarkAsProcessed(messageId);

        // DeliveryStatus update was persisted before the rethrow.
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
