using ContentTours.Domain.Enums;

namespace ContentTours.Application.Commands.Shared;

public static class ChildFacilitiesParser
{
    private static readonly char[] CsvSeparators = [',', ';'];

    /// <summary>
    /// Parses a CSV/semicolon-separated list of facility tokens (matches the current
    /// <c>UpdateChildrenInfoRequest.ChildFacilities</c> request shape).
    /// </summary>
    public static bool TryParse(
        string? csv,
        out IReadOnlyList<ChildFacility> facilities,
        out string? unknownToken)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            facilities = Array.Empty<ChildFacility>();
            unknownToken = null;
            return true;
        }

        var tokens = csv.Split(CsvSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return TryParse(tokens, out facilities, out unknownToken);
    }

    /// <summary>
    /// Parses an explicit token list. Accepts <c>null</c> as empty.
    /// </summary>
    public static bool TryParse(
        IEnumerable<string>? tokens,
        out IReadOnlyList<ChildFacility> facilities,
        out string? unknownToken)
    {
        if (tokens is null)
        {
            facilities = Array.Empty<ChildFacility>();
            unknownToken = null;
            return true;
        }

        var seen = new HashSet<ChildFacility>();
        var parsed = new List<ChildFacility>();

        foreach (var raw in tokens)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var trimmed = raw.Trim();

            if (!Enum.TryParse<ChildFacility>(trimmed, ignoreCase: true, out var facility)
                || !Enum.IsDefined(typeof(ChildFacility), facility))
            {
                facilities = Array.Empty<ChildFacility>();
                unknownToken = trimmed;
                return false;
            }

            if (seen.Add(facility))
                parsed.Add(facility);
        }

        // Deterministic ordering by underlying enum value (byte) so persisted rows + event
        // payloads are stable across calls regardless of input ordering.
        parsed.Sort();

        facilities = parsed;
        unknownToken = null;
        return true;
    }
}
