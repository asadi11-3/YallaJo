using Auth.Application.Recaptcha;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Tests.Unit;

/// <summary>
/// Contract tests for the MediatR pipeline guard that centralizes reCAPTCHA
/// enforcement. Verifies that the behavior short-circuits every negative case
/// with a generic failure and never invokes the next handler.
/// </summary>
public sealed class RecaptchaValidationBehaviorTests
{
    private sealed record ProtectedCommand(string RecaptchaToken) :
        ICommand<string>, IRecaptchaProtectedCommand
    {
        public string RecaptchaAction => "test_action";
    }

    private sealed record ProtectedVoidCommand(string RecaptchaToken) :
        ICommand, IRecaptchaProtectedCommand
    {
        public string RecaptchaAction => "test_action";
    }

    private sealed record UnprotectedCommand : ICommand<string>;

    private readonly IRecaptchaVerifier _verifier = Substitute.For<IRecaptchaVerifier>();
    private readonly IRequestContext _requestContext = Substitute.For<IRequestContext>();

    private RecaptchaValidationBehavior<TRequest, TResponse> Behavior<TRequest, TResponse>()
        where TRequest : notnull =>
        new(_verifier, _requestContext,
            NullLogger<RecaptchaValidationBehavior<TRequest, TResponse>>.Instance);

    private static RequestHandlerDelegate<T> DelegateReturning<T>(T value, Action? onCalled = null) =>
        _ =>
        {
            onCalled?.Invoke();
            return Task.FromResult(value);
        };

    [Fact]
    public async Task Handle_ShouldSkipVerifier_WhenCommandNotProtected()
    {
        var behavior = Behavior<UnprotectedCommand, Result<string>>();

        var result = await behavior.Handle(
            new UnprotectedCommand(),
            DelegateReturning(Result<string>.Success("ok")),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _verifier.DidNotReceiveWithAnyArgs().VerifyAsync(default!, default!, default, default);
    }

    [Fact]
    public async Task Handle_ShouldShortCircuit_WhenTokenMissing()
    {
        var behavior = Behavior<ProtectedCommand, Result<string>>();
        var nextCalled = false;

        var result = await behavior.Handle(
            new ProtectedCommand(string.Empty),
            DelegateReturning(Result<string>.Success("never"), () => nextCalled = true),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Forbidden);
        nextCalled.Should().BeFalse();
        await _verifier.DidNotReceiveWithAnyArgs().VerifyAsync(default!, default!, default, default);
    }

    [Fact]
    public async Task Handle_ShouldShortCircuit_WhenVerifierFails()
    {
        _verifier.VerifyAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(Error.Failure("Recaptcha.VerificationFailed", "bad"), Outcome.Forbidden));

        var behavior = Behavior<ProtectedCommand, Result<string>>();
        var nextCalled = false;

        var result = await behavior.Handle(
            new ProtectedCommand("some-token"),
            DelegateReturning(Result<string>.Success("never"), () => nextCalled = true),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Forbidden);
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldInvokeNext_WhenVerifierSucceeds()
    {
        _verifier.VerifyAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var behavior = Behavior<ProtectedCommand, Result<string>>();

        var result = await behavior.Handle(
            new ProtectedCommand("good-token"),
            DelegateReturning(Result<string>.Success("handled")),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("handled");
    }

    [Fact]
    public async Task Handle_ShouldShortCircuit_ForVoidCommand_WithGenericFailure()
    {
        _verifier.VerifyAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(Error.Failure("Recaptcha.VerificationFailed", "bad"), Outcome.Forbidden));

        var behavior = Behavior<ProtectedVoidCommand, Result>();

        var result = await behavior.Handle(
            new ProtectedVoidCommand("t"),
            DelegateReturning(Result.Success()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Forbidden);
    }
}
