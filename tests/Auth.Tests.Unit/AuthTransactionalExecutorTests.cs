using Auth.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Auth.Tests.Unit;

/// <summary>
/// Verifies <see cref="AuthTransactionalExecutor"/> uses the DbContext's
/// execution strategy so it is compatible with
/// <c>EnableRetryOnFailure</c>-configured providers.
/// <para>
/// The regression this guards against:
/// </para>
/// <code>
/// System.InvalidOperationException: The configured execution strategy
/// 'SqlServerRetryingExecutionStrategy' does not support user-initiated
/// transactions. Use the execution strategy returned by
/// 'DbContext.Database.CreateExecutionStrategy()' …
/// </code>
/// <para>
/// This would previously fire against production SQL Server the moment
/// <see cref="System.Transactions.TransactionScope"/> was entered while the
/// retrying strategy was configured. The fix routes the delegate through
/// <c>DbContext.Database.CreateExecutionStrategy().ExecuteAsync(...)</c>.
/// </para>
/// </summary>
public sealed class AuthTransactionalExecutorTests
{
    private static AuthDbContext CreateContext(string name)
    {
        var opts = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new AuthDbContext(opts);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldInvokeDelegateAndReturnResult()
    {
        using var db = CreateContext($"tx-ok-{Guid.NewGuid()}");
        var sut = new AuthTransactionalExecutor(db);

        var result = await sut.ExecuteAsync(
            operation: _ => Task.FromResult(42),
            ct: CancellationToken.None);

        result.Should().Be(42);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPropagateException_AndNotCompleteScope()
    {
        using var db = CreateContext($"tx-fail-{Guid.NewGuid()}");
        var sut = new AuthTransactionalExecutor(db);

        var act = async () => await sut.ExecuteAsync<int>(
            operation: _ => throw new InvalidOperationException("boom"),
            ct: CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("boom");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReceiveLinkedCancellationToken()
    {
        using var db = CreateContext($"tx-ct-{Guid.NewGuid()}");
        var sut = new AuthTransactionalExecutor(db);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        CancellationToken observed = default;
        var act = async () => await sut.ExecuteAsync<int>(
            operation: innerCt =>
            {
                observed = innerCt;
                innerCt.ThrowIfCancellationRequested();
                return Task.FromResult(0);
            },
            ct: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        observed.IsCancellationRequested.Should().BeTrue(
            "the executor must flow the caller's CancellationToken into the delegate");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenOperationIsNull()
    {
        using var db = CreateContext($"tx-null-{Guid.NewGuid()}");
        var sut = new AuthTransactionalExecutor(db);

        var act = async () => await sut.ExecuteAsync<int>(
            operation: null!,
            ct: CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
