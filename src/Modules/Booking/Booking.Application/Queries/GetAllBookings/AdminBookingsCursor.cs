using System.Buffers.Text;
using System.Globalization;
using System.Text;

namespace Booking.Application.Queries.GetAllBookings;

/// <summary>
/// Opaque cursor for admin booking pagination.
/// Mirrors <c>MyBookingsCursor</c> shape: <c>(CreatedAt, Id)</c> tuple encoded as URL-safe base64.
/// Kept as a distinct type so the admin and self-scoped lists can evolve independently
/// (e.g., admin might later need to add provider/tenant filters into the cursor payload).
/// </summary>
internal sealed record AdminBookingsCursor(Guid Id, DateTime CreatedAt)
{
    private const string Separator = "|";

    public string Encode()
    {
        var payload = $"{CreatedAt.ToString("O", CultureInfo.InvariantCulture)}{Separator}{Id.ToString("D", CultureInfo.InvariantCulture)}";
        var bytes = Encoding.UTF8.GetBytes(payload);
        return Base64Url.EncodeToString(bytes);
    }

    public static bool TryDecode(string? raw, out AdminBookingsCursor? cursor)
    {
        cursor = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return true; // null cursor = first page
        }

        try
        {
            var bytes = Base64Url.DecodeFromChars(raw.AsSpan());
            var payload = Encoding.UTF8.GetString(bytes);
            var parts = payload.Split(Separator);
            if (parts.Length != 2)
            {
                return false;
            }

            if (!DateTime.TryParse(
                    parts[0],
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind | DateTimeStyles.AdjustToUniversal,
                    out var createdAt))
            {
                return false;
            }

            if (!Guid.TryParseExact(parts[1], "D", out var id))
            {
                return false;
            }

            cursor = new AdminBookingsCursor(id, createdAt);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
