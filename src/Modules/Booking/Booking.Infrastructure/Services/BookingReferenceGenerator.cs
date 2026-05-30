using System.Security.Cryptography;
using Booking.Application.Interfaces;
using Booking.Domain.Repositories;
using Booking.Domain.ValueObjects;

namespace Booking.Infrastructure.Services;

/// <summary>
/// Generates human-readable, collision-resistant booking references in the
/// format <c>YJ-YYYYMMDD-XXXXXX</c> using the Crockford Base32 alphabet
/// (omits visually-ambiguous chars: <c>O, 0, I, 1</c>).
/// </summary>
/// <remarks>
/// Strategy: pull 6 cryptographic random bytes, map each byte mod 32 onto the
/// alphabet, format the candidate, then probe the DB for collisions.
/// Retry up to 5 times before failing — at 32^6 = ~1.07B daily references
/// the collision probability is astronomically low. A 5x retry is more than
/// enough; if we ever exhaust it the system is almost certainly mis-configured
/// (e.g. clock skewing into the past), so we throw rather than silently degrade.
/// </remarks>
internal sealed class BookingReferenceGenerator(ITourBookingRepository repository)
    : IBookingReferenceGenerator
{
    private const string CrockfordAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int RandomPartLength = 6;
    private const int MaxRetries = 5;

    public async Task<BookingReference> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var dateUtc = DateOnly.FromDateTime(DateTime.UtcNow);

        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            var random = GenerateRandomSegment();
            var candidate = BookingReference.Compose(dateUtc, random);

            var existing = await repository
                .GetByReferenceAsync(candidate.Value, cancellationToken)
                .ConfigureAwait(false);

            if (existing is null)
            {
                return candidate;
            }
        }

        throw new InvalidOperationException(
            $"Failed to generate a unique BookingReference after {MaxRetries} attempts. " +
            "This indicates either an extreme collision (statistically impossible at normal volumes) " +
            "or DB-level corruption of the BookingReference index.");
    }

    private static string GenerateRandomSegment()
    {
        Span<byte> bytes = stackalloc byte[RandomPartLength];
        RandomNumberGenerator.Fill(bytes);

        Span<char> chars = stackalloc char[RandomPartLength];
        for (var i = 0; i < RandomPartLength; i++)
        {
            chars[i] = CrockfordAlphabet[bytes[i] % CrockfordAlphabet.Length];
        }

        return new string(chars);
    }
}
