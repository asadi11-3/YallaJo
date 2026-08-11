using System.Globalization;
using System.Text;

namespace Tracking.Application.Queries.GetTrackingHistory;

internal sealed record TrackingHistoryCursor(Guid Id, DateTime StartedAt)
{
    public string Encode()
    {
        var raw = $"{StartedAt:O}|{Id:D}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    public static bool TryDecode(string? value, out TrackingHistoryCursor? cursor)
    {
        cursor = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        try
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(value));
            var parts = raw.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                return false;
            }

            if (!DateTime.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var startedAt))
            {
                return false;
            }

            if (!Guid.TryParse(parts[1], out var id))
            {
                return false;
            }

            cursor = new TrackingHistoryCursor(id, startedAt);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
