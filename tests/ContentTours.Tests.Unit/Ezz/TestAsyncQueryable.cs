using System.Collections;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace ContentTours.Tests.Unit.Ezz;

/// <summary>
/// Wraps a plain <see cref="IEnumerable{T}"/> in an <see cref="IQueryable{T}"/> that
/// also implements <see cref="IAsyncQueryProvider"/>, allowing EF Core extensions such
/// as <c>MaxAsync</c> and <c>ToListAsync</c> to work against in-memory data in pure
/// NSubstitute unit tests (no real DbContext required).
/// </summary>
internal sealed class TestAsyncQueryable<T> : IOrderedQueryable<T>, IAsyncEnumerable<T>
{
    private readonly IQueryable<T> _inner;

    public TestAsyncQueryable(IEnumerable<T> source)
        => _inner = source.AsQueryable();

    public Type ElementType => _inner.ElementType;
    public Expression Expression => _inner.Expression;
    public IQueryProvider Provider => new TestAsyncQueryProvider<T>(_inner);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _inner.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken ct = default)
        => new TestAsyncEnumerator<T>(_inner.GetEnumerator());
}

internal sealed class TestAsyncQueryProvider<T>(IQueryable<T> inner)
    : IQueryProvider, IAsyncQueryProvider
{
    public IQueryable CreateQuery(Expression expression)
        => new TestAsyncQueryable<object>(inner.Provider.CreateQuery<object>(expression));

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        => new TestAsyncQueryable<TElement>(inner.Provider.CreateQuery<TElement>(expression));

    public object? Execute(Expression expression)
        => inner.Provider.Execute(expression);

    public TResult Execute<TResult>(Expression expression)
        => inner.Provider.Execute<TResult>(expression);

    /// <summary>
    /// EF Core calls this for async operators like <c>MaxAsync</c>, <c>FirstOrDefaultAsync</c>,
    /// etc. We execute synchronously using the inner LINQ provider and wrap in a completed Task.
    /// </summary>
    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken ct = default)
    {
        // TResult is typically Task<TValue>. Extract the inner value type.
        var resultType = typeof(TResult).GetGenericArguments()[0];

        // IQueryProvider has two Execute overloads; pick the generic one specifically
        // to avoid AmbiguousMatchException from GetMethod(name, parameterTypes).
        var executeGeneric = typeof(IQueryProvider)
            .GetMethods()
            .Single(m => m.Name == nameof(IQueryProvider.Execute) && m.IsGenericMethod)
            .MakeGenericMethod(resultType);

        var syncResult = executeGeneric.Invoke(inner.Provider, [expression]);

        return (TResult)typeof(Task)
            .GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(resultType)
            .Invoke(null, [syncResult])!;
    }
}

internal sealed class TestAsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
{
    public T Current => inner.Current;
    public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(inner.MoveNext());
    public ValueTask DisposeAsync() { inner.Dispose(); return ValueTask.CompletedTask; }
}
