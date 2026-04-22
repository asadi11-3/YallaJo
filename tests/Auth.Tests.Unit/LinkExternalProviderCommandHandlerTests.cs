using System.Linq.Expressions;
using Auth.Application.Commands.LinkExternalProvider;
using Auth.Application.ExternalAuth;
using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Tests.Unit;

/// <summary>
/// Security-focused unit tests for <see cref="LinkExternalProviderCommandHandler"/>.
/// </summary>
public sealed class LinkExternalProviderCommandHandlerTests
{
    private readonly IExternalProviderRepository _repo = Substitute.For<IExternalProviderRepository>();
    private readonly IAuthUnitOfWork _uow = Substitute.For<IAuthUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IExternalAuthTicketVerifier _verifier = Substitute.For<IExternalAuthTicketVerifier>();
    private readonly IExternalAuthNonceStore _nonceStore = Substitute.For<IExternalAuthNonceStore>();

    private LinkExternalProviderCommandHandler CreateSut() =>
        new(_repo, _uow, _currentUser, _verifier, _nonceStore,
            NullLogger<LinkExternalProviderCommandHandler>.Instance);

    private static ExternalAuthTicket SampleTicket(
        string provider = "google",
        string providerUserId = "google-user-123") =>
        new(
            TicketId: Guid.NewGuid(),
            Provider: provider,
            ProviderUserId: providerUserId,
            Email: "user@gmail.com",
            EmailVerifiedByProvider: true,
            IssuedAt: DateTime.UtcNow,
            ExpiresAt: DateTime.UtcNow.AddMinutes(2));

    private void AuthenticateUser(Guid userId)
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(userId);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenUnauthenticated()
    {
        _currentUser.IsAuthenticated.Returns(false);

        var sut = CreateSut();

        var result = await sut.Handle(new LinkExternalProviderCommand("any", "test-recaptcha-token"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Unauthorized);
        _verifier.DidNotReceiveWithAnyArgs().Verify(default!);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenTicketInvalid()
    {
        AuthenticateUser(Guid.NewGuid());
        _verifier.Verify(Arg.Any<string>())
            .Returns(Result<ExternalAuthTicket>.Failure(
                Error.Unauthorized("Signature invalid."), Outcome.Unauthorized));

        var sut = CreateSut();

        var result = await sut.Handle(new LinkExternalProviderCommand("tampered", "test-recaptcha-token"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Unauthorized);
        await _nonceStore.DidNotReceiveWithAnyArgs()
            .TryConsumeAsync(default, default, default);
        await _repo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_ShouldReject_OnReplay_WhenNonceAlreadyConsumed()
    {
        AuthenticateUser(Guid.NewGuid());
        var ticket = SampleTicket();
        _verifier.Verify(Arg.Any<string>()).Returns(Result<ExternalAuthTicket>.Success(ticket));
        _nonceStore.TryConsumeAsync(ticket.TicketId, ticket.ExpiresAt, Arg.Any<CancellationToken>())
            .Returns(false);

        var sut = CreateSut();

        var result = await sut.Handle(new LinkExternalProviderCommand("t", "test-recaptcha-token"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Unauthorized);
        await _repo.DidNotReceiveWithAnyArgs().AnyAsync(
            filter: default!, ct: default);
        await _repo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenUserAlreadyActiveLinkForProvider()
    {
        var userId = Guid.NewGuid();
        AuthenticateUser(userId);
        var ticket = SampleTicket();

        _verifier.Verify(Arg.Any<string>()).Returns(Result<ExternalAuthTicket>.Success(ticket));
        _nonceStore.TryConsumeAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // First AnyAsync call (self has active link) → true.
        _repo.AnyAsync(
                Arg.Any<Expression<Func<ExternalProvider, bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var sut = CreateSut();

        var result = await sut.Handle(new LinkExternalProviderCommand("t", "test-recaptcha-token"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().Contain(e => e.Code.Contains("AlreadyLinked", StringComparison.Ordinal));
        await _repo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenProviderIdTakenByAnotherUser()
    {
        var userId = Guid.NewGuid();
        AuthenticateUser(userId);
        var ticket = SampleTicket();

        _verifier.Verify(Arg.Any<string>()).Returns(Result<ExternalAuthTicket>.Success(ticket));
        _nonceStore.TryConsumeAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // First call (self-link) false, second call (other-owner) true.
        _repo.AnyAsync(
                Arg.Any<Expression<Func<ExternalProvider, bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(false, true);

        var sut = CreateSut();

        var result = await sut.Handle(new LinkExternalProviderCommand("t", "test-recaptcha-token"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors.Should().Contain(e => e.Code.Contains("ProviderIdTaken", StringComparison.Ordinal));
        await _repo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_ShouldCreateLink_WhenAllGuardsPass()
    {
        var userId = Guid.NewGuid();
        AuthenticateUser(userId);
        var ticket = SampleTicket();

        _verifier.Verify(Arg.Any<string>()).Returns(Result<ExternalAuthTicket>.Success(ticket));
        _nonceStore.TryConsumeAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _repo.AnyAsync(Arg.Any<Expression<Func<ExternalProvider, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        ExternalProvider? captured = null;
        _repo.AddAsync(Arg.Do<ExternalProvider>(e => captured = e), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        var result = await sut.Handle(new LinkExternalProviderCommand("t", "test-recaptcha-token"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.UserId.Should().Be(userId);
        captured.Provider.Should().Be("google");
        captured.ProviderUserId.Should().Be(ticket.ProviderUserId);
        captured.IsActive.Should().BeTrue();
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldLowercaseProviderClaim_EvenWhenTicketHasCasing()
    {
        // The verifier itself normalizes, but the handler must defensively
        // lowercase the provider claim anyway so that storage is consistent
        // and the filtered unique index works.
        var userId = Guid.NewGuid();
        AuthenticateUser(userId);
        var ticket = SampleTicket(provider: "GOOGLE");

        _verifier.Verify(Arg.Any<string>()).Returns(Result<ExternalAuthTicket>.Success(ticket));
        _nonceStore.TryConsumeAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _repo.AnyAsync(Arg.Any<Expression<Func<ExternalProvider, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        ExternalProvider? captured = null;
        _repo.AddAsync(Arg.Do<ExternalProvider>(e => captured = e), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        await sut.Handle(new LinkExternalProviderCommand("t", "test-recaptcha-token"), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Provider.Should().Be("google");
    }

    [Fact]
    public async Task Handle_ShouldConsumeNonce_BeforeMutating()
    {
        // Prevents replay races: if nonce consumption is delayed after DB
        // writes, a second concurrent request could slip through.
        var userId = Guid.NewGuid();
        AuthenticateUser(userId);
        var ticket = SampleTicket();

        _verifier.Verify(Arg.Any<string>()).Returns(Result<ExternalAuthTicket>.Success(ticket));
        _nonceStore.TryConsumeAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _repo.AnyAsync(Arg.Any<Expression<Func<ExternalProvider, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var sut = CreateSut();

        await sut.Handle(new LinkExternalProviderCommand("t", "test-recaptcha-token"), CancellationToken.None);

        // NSubstitute's InOrder is strict on matcher equality; the handler makes
        // two AnyAsync calls so we relax to a sequence of event categories.
        Received.InOrder(() =>
        {
            _ = _nonceStore.TryConsumeAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
            _ = _repo.AddAsync(Arg.Any<ExternalProvider>(), Arg.Any<CancellationToken>());
            _ = _uow.SaveChangesAsync(Arg.Any<CancellationToken>());
        });
    }
}
