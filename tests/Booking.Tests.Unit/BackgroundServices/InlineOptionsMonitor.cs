using Microsoft.Extensions.Options;

namespace Booking.Tests.Unit.BackgroundServices;

/// <summary>
/// Trivial <see cref="IOptionsMonitor{T}"/> wrapper around a fixed instance — used to inject
/// an options snapshot into a Booking background service during unit tests.
/// </summary>
internal sealed class InlineOptionsMonitor<T>(T value) : IOptionsMonitor<T> where T : class
{
    public T CurrentValue { get; } = value;

    public T Get(string? name) => CurrentValue;

    public IDisposable? OnChange(Action<T, string?> listener) => null;
}
