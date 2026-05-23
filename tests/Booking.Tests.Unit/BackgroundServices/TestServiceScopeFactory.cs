using Microsoft.Extensions.DependencyInjection;

namespace Booking.Tests.Unit.BackgroundServices;

/// <summary>
/// Minimal in-memory <see cref="IServiceScopeFactory"/> so we can drive a Booking background
/// service's <c>RunOnceAsync</c> without a real DI container. Every <c>CreateScope</c> call
/// returns the SAME ServiceProvider, which is exactly what the per-scope service resolution
/// in the BG services needs for unit tests.
/// </summary>
internal sealed class TestServiceScopeFactory : IServiceScopeFactory, IServiceScope, IServiceProvider
{
    private readonly Dictionary<Type, object> _services;

    public TestServiceScopeFactory(IReadOnlyDictionary<Type, object> services)
    {
        _services = new Dictionary<Type, object>(services);
    }

    public IServiceProvider ServiceProvider => this;

    public IServiceScope CreateScope() => this;

    public object? GetService(Type serviceType)
        => _services.TryGetValue(serviceType, out var instance) ? instance : null;

    public void Dispose()
    {
        // No-op — caller still holds the substitutes; nothing to release here.
    }
}
