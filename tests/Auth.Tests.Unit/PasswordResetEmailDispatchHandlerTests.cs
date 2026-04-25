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
/// <see cref="PasswordResetEmailDispatchHandler"/> is the only place
/// SMTP runs on the password-reset path. Same shape as
/// <c>ActivationEmailDispatchHandlerTests</c> — both handlers follow
/// the identical idempotency + retry pattern.
/// </summary>
public sealed class PasswordResetEmailDispatchHandlerTests
{
    private readonly IPasswordResetTokenRepository _tokenRepo  = Substitute.For<IPasswordResetTokenRepository>();
    private readonly IAuthInboxStore               _inboxStore = Substitute.For<IAuthInboxStore>();
    private readonly IAuthUnitOfWork               _uow        = Substitute.For<IAuthUnitOfWork>();
    private readonly IEmailService                 _email      = Substitute.For<IEmailService>();

    private PasswordResetEmailDispatchHandler CreateSut() =>
        new(_tokenRepo, _inboxStore, _uow, _email,
            NullLogger<PasswordResetEmailDispatchHandler>.Instance);

    private static PasswordResetTokenIssuedIntegrationEvent Event(Guid tokenId, Guid userId,
        string email = "user@example.com", string plain = "909090",
        PasswordResetOriginSnapshot origin = PasswordResetOriginSnapshot.SelfService) =>
        new(
            TokenId:         tokenId,
            UserId:          userId,
            DeliveryAddress: email,
            PlainCode:       plain,
            ExpiresAt:       DateTime.UtcNow.AddMinutes(10),
            Origin:          origin);

    private static IntegrationEventNotification<PasswordResetTokenIssuedIntegrationEvent> Notification(
        Guid messageId, PasswordResetTokenIssuedIntegrationEvent ev) => new(messageId, ev);

    private void StubInboxProcessed(Guid messageId, bool processed) =>
        _inboxStore.HasBeenProcessedAsync(messageId, Arg.Any<CancellationToken>()).Returns(processed);

    private void StubToken(Guid tokenId, PasswordResetToken? token) =>
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

        _inboxStore.Received(1).MarkAsProcessed(messageId);
        await _email.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldSkipEmail_WhenTokenIsConsumed()
    {
        var messageId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var token = PasswordResetToken.Issue(userId, "h", "user@example.com", 10);
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

        var token = PasswordResetToken.Issue(userId, "h", "user@example.com", 10);
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

        var token = PasswordResetToken.Issue(userId, "h", "user@example.com", 10);
        typeof(PasswordResetToken).GetProperty(nameof(PasswordResetToken.ExpiresAt))!
            .GetSetMethod(nonPublic: true)!
            .Invoke(token, new object[] { DateTime.UtcNow.AddMinutes(-1) });

        StubInboxProcessed(messageId, false);
        StubToken(token.Id, token);

        var sut = CreateSut();

        await sut.Handle(Notification(messageId, Event(token.Id, userId)), CancellationToken.None);

        await _email.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        _inboxStore.Received(1).MarkAsProcessed(messageId);
    }

    [Fact]
    public async Task Handle_ShouldSkipDuplicateSend_WhenTokenAlreadyDelivered()
    {
        var messageId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var token = PasswordResetToken.Issue(userId, "h", "user@example.com", 10);
        token.MarkDelivered();

        StubInboxProcessed(messageId, false);
        StubToken(token.Id, token);

        var sut = CreateSut();

        await sut.Handle(Notification(messageId, Event(token.Id, userId)), CancellationToken.None);

        await _email.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        _inboxStore.Received(1).MarkAsProcessed(messageId);
    }

    [Fact]
    public async Task Handle_OnHappyPath_ShouldSendEmail_MarkDelivered_AndMarkInbox()
    {
        var messageId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var token = PasswordResetToken.Issue(userId, "h", "user@example.com", 10);
        StubInboxProcessed(messageId, false);
        StubToken(token.Id, token);

        var ev = Event(token.Id, userId, email: "user@example.com", plain: "909090");

        var sut = CreateSut();

        await sut.Handle(Notification(messageId, ev), CancellationToken.None);

        await _email.Received(1).SendAsync(
            "user@example.com",
            Arg.Any<string>(),
            Arg.Is<string>(body => body.Contains("909090", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());

        token.State.Should().Be(PasswordResetTokenState.Delivered);
        token.DeliveryStatus.Should().Be(PasswordResetTokenDeliveryStatus.Sent);
        token.LastSentAt.Should().NotBeNull();

        _inboxStore.Received(1).MarkAsProcessed(messageId);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnSmtpFailure_ShouldMarkDeliveryFailed_NotMarkInbox_AndRethrow()
    {
        var messageId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var token = PasswordResetToken.Issue(userId, "h", "user@example.com", 10);
        StubInboxProcessed(messageId, false);
        StubToken(token.Id, token);

        _email.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => Task.FromException(new InvalidOperationException("SMTP DOWN")));

        var sut = CreateSut();

        var act = () => sut.Handle(Notification(messageId, Event(token.Id, userId)), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();

        token.State.Should().Be(PasswordResetTokenState.Issued,
            "Phase 2C-3: transient SMTP failures must NOT revoke the token");
        token.DeliveryStatus.Should().Be(PasswordResetTokenDeliveryStatus.Failed);
        token.LastSentAt.Should().NotBeNull();

        _inboxStore.DidNotReceive().MarkAsProcessed(messageId);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Phase 3A: origin-based wording branch ────────────────────────────────

    [Fact]
    public async Task Handle_OnSelfServiceOrigin_ShouldUseSelfServiceWording()
    {
        var (messageId, token) = await SetupHappyPath();

        var sut = CreateSut();

        await sut.Handle(
            Notification(messageId, Event(token.Id, token.UserId, plain: "SELF123",
                origin: PasswordResetOriginSnapshot.SelfService)),
            CancellationToken.None);

        await _email.Received(1).SendAsync(
            Arg.Any<string>(),
            Arg.Is<string>(s => s.Contains("Reset Your Password", StringComparison.Ordinal)),
            Arg.Is<string>(b => b.Contains("Your password reset code is: SELF123", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnAdminInitiatedOrigin_ShouldUseAdminWording()
    {
        // Phase 3A — admin-initiated reset uses its own subject + body
        // so the user sees clearly that an administrator initiated the
        // flow (and is nudged to contact admin if unexpected).
        var (messageId, token) = await SetupHappyPath();

        var sut = CreateSut();

        await sut.Handle(
            Notification(messageId, Event(token.Id, token.UserId, plain: "ADMIN42",
                origin: PasswordResetOriginSnapshot.AdminInitiated)),
            CancellationToken.None);

        await _email.Received(1).SendAsync(
            Arg.Any<string>(),
            Arg.Is<string>(s => s.Contains("Administrator-initiated", StringComparison.Ordinal)),
            Arg.Is<string>(b =>
                b.Contains("administrator has initiated", StringComparison.Ordinal)
             && b.Contains("ADMIN42", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnReassignmentOrigin_ShouldUseReassignmentWording()
    {
        // Reassignment wording is reserved for a later phase but the
        // branch exists today; exercise it to prevent drift.
        var (messageId, token) = await SetupHappyPath();

        var sut = CreateSut();

        await sut.Handle(
            Notification(messageId, Event(token.Id, token.UserId, plain: "REAS99",
                origin: PasswordResetOriginSnapshot.Reassignment)),
            CancellationToken.None);

        await _email.Received(1).SendAsync(
            Arg.Any<string>(),
            Arg.Is<string>(s => s.Contains("reassignment", StringComparison.OrdinalIgnoreCase)),
            Arg.Is<string>(b => b.Contains("REAS99", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());
    }

    private async Task<(Guid messageId, PasswordResetToken token)> SetupHappyPath()
    {
        var messageId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var token = PasswordResetToken.Issue(userId, "h", "user@example.com", 10);

        StubInboxProcessed(messageId, false);
        StubToken(token.Id, token);

        await Task.CompletedTask;
        return (messageId, token);
    }
}
