using System.Text.RegularExpressions;

namespace Booking.Domain.ValueObjects;

/// <summary>
/// Human-readable booking reference of the form <c>YJ-YYYYMMDD-XXXXXX</c>
/// where XXXXXX is 6 characters drawn from <see cref="Alphabet"/>
/// (Crockford-style base32, excluding the ambiguous glyphs O, 0, I, 1).
/// </summary>
public readonly partial record struct BookingReference
{
    /// <summary>Allowed character alphabet (32 chars). Excludes O, 0, I, 1 to avoid OCR/handwriting confusion.</summary>
    public const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    [GeneratedRegex(@"^YJ-\d{8}-[ABCDEFGHJKLMNPQRSTUVWXYZ23456789]{6}$", RegexOptions.Compiled)]
    private static partial Regex FormatRegex();

    /// <summary>The fully formatted reference string.</summary>
    public string Value { get; }

    private BookingReference(string value) => Value = value;

    /// <summary>
    /// Wrap an externally-produced reference string after validating its shape.
    /// </summary>
    public static BookingReference From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Booking reference cannot be null or whitespace.", nameof(value));
        if (!FormatRegex().IsMatch(value))
            throw new ArgumentException($"Invalid booking reference format: '{value}'. Expected YJ-YYYYMMDD-XXXXXX.", nameof(value));
        return new BookingReference(value);
    }

    /// <summary>
    /// Compose a reference from a date + suffix without re-validating each character.
    /// Used by the generator; suffix must already be 6 chars from <see cref="Alphabet"/>.
    /// </summary>
    public static BookingReference Compose(DateOnly date, string suffix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(suffix);
        if (suffix.Length != 6)
            throw new ArgumentException("Suffix must be exactly 6 characters.", nameof(suffix));
        foreach (var c in suffix)
        {
            if (!Alphabet.Contains(c))
                throw new ArgumentException($"Suffix character '{c}' not in allowed alphabet.", nameof(suffix));
        }

        var formatted = $"YJ-{date:yyyyMMdd}-{suffix}";
        return new BookingReference(formatted);
    }

    public override string ToString() => Value;

    public static implicit operator string(BookingReference reference) => reference.Value;
}
