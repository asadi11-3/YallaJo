namespace Booking.Tests.Unit.BackgroundServices;

/// <summary>
/// Minimal in-test <see cref="TimeProvider"/>. The full <c>Microsoft.Extensions.TimeProvider.Testing</c>
/// package is not referenced from <c>Booking.Tests.Unit</c>; this stub is sufficient for the
/// fixed-time scenarios our BG-service tests need.
/// </summary>
internal sealed class FakeTimeProvider(DateTimeOffset initialNowUtc) : TimeProvider
{
    private DateTimeOffset _nowUtc = initialNowUtc;

    public override DateTimeOffset GetUtcNow() => _nowUtc;

    public void Advance(TimeSpan delta) => _nowUtc = _nowUtc.Add(delta);

    public void Set(DateTimeOffset newUtc) => _nowUtc = newUtc;
}
