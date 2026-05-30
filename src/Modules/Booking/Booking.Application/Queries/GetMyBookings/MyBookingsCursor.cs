using System.Buffers.Text;
using System.Globalization;
using System.Text;

namespace Booking.Application.Queries.GetMyBookings;

/// <summary>
/// Opaque cursor for booking list pagination.
/// Encoded as URL-safe base64 of "{createdAt:O}|{id:D}".
/// Order is DESC by CreatedAt then ASC by Id (per B-R10).
/// </summary>
internal sealed record MyBookingsCursor(Guid Id, DateTime CreatedAt)
{
    public string Encode()
    {
        var payload = string.Create(
            CultureInfo.InvariantCulture,
            $"{CreatedAt:O}|{Id:D}");
        var bytes = Encoding.UTF8.GetBytes(payload);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    public static bool TryDecode(string? value, out MyBookingsCursor? cursor)
    {
        cursor = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true; // null is valid: first page.
        }

        try
        {
            var padded = value.Replace('-', '+').Replace('_', '/');
            var padding = padded.Length % 4;
            if (padding > 0)
            {
                padded = padded.PadRight(padded.Length + (4 - padding), '=');
            }

            var bytes = Convert.FromBase64String(padded);
            var payload = Encoding.UTF8.GetString(bytes);
            var separator = payload.IndexOf('|');
            if (separator < 1 || separator == payload.Length - 1)
            {
                return false;
            }

            var createdAtRaw = payload[..separator];
            var idRaw = payload[(separator + 1)..];

            if (!DateTime.TryParse(
                createdAtRaw,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind | DateTimeStyles.AdjustToUniversal,
                out var createdAt))
            {
                return false;
            }

            if (!Guid.TryParse(idRaw, out var id))
            {
                return false;
            }

            cursor = new MyBookingsCursor(id, DateTime.SpecifyKind(createdAt, DateTimeKind.Utc));
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
