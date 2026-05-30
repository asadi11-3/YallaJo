using System.Globalization;
using System.Text;

namespace Booking.Application.Queries.GetAvailabilityForTour;

internal sealed record AvailabilityCursor(Guid Id, DateOnly Date)
{
    public string Encode()
    {
        var payload = string.Create(CultureInfo.InvariantCulture, $"{Date:yyyy-MM-dd}|{Id:D}");
        var bytes = Encoding.UTF8.GetBytes(payload);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    public static bool TryDecode(string? value, out AvailabilityCursor? cursor)
    {
        cursor = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
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

            var dateRaw = payload[..separator];
            var idRaw = payload[(separator + 1)..];

            if (!DateOnly.TryParseExact(dateRaw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                return false;
            }

            if (!Guid.TryParse(idRaw, out var id))
            {
                return false;
            }

            cursor = new AvailabilityCursor(id, date);
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
