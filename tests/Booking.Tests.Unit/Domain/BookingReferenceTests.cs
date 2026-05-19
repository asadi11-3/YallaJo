using Booking.Domain.ValueObjects;
using FluentAssertions;

namespace Booking.Tests.Unit.Domain;

/// <summary>
/// Unit tests for <see cref="BookingReference"/> value object.
/// Format: <c>YJ-YYYYMMDD-XXXXXX</c> with Crockford base32 alphabet (excludes O, 0, I, 1).
/// </summary>
public sealed class BookingReferenceTests
{
    [Fact]
    public void Compose_with_valid_inputs_returns_formatted_reference()
    {
        var date = new DateOnly(2026, 7, 15);
        var reference = BookingReference.Compose(date, "A7X3K9");

        reference.Value.Should().Be("YJ-20260715-A7X3K9");
    }

    [Fact]
    public void Compose_implicitly_converts_to_string()
    {
        var reference = BookingReference.Compose(new DateOnly(2026, 1, 1), "BCDEFG");

        string asString = reference;
        asString.Should().Be("YJ-20260101-BCDEFG");
    }

    [Theory]
    [InlineData("ABCDE")]   // too short
    [InlineData("ABCDEFG")] // too long
    [InlineData("")]
    [InlineData("   ")]
    public void Compose_throws_when_suffix_length_is_invalid(string suffix)
    {
        var act = () => BookingReference.Compose(new DateOnly(2026, 1, 1), suffix);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("ABCDEO")] // contains O (excluded)
    [InlineData("ABCD01")] // contains 0 and 1 (excluded)
    [InlineData("ABCDEI")] // contains I (excluded)
    [InlineData("abcdef")] // lowercase not allowed
    public void Compose_throws_when_suffix_contains_disallowed_character(string suffix)
    {
        var act = () => BookingReference.Compose(new DateOnly(2026, 1, 1), suffix);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void From_valid_reference_string_succeeds()
    {
        var reference = BookingReference.From("YJ-20260615-A7X3K9");

        reference.Value.Should().Be("YJ-20260615-A7X3K9");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("YJ-2026-A7X3K9")]           // wrong date format
    [InlineData("XX-20260615-A7X3K9")]       // wrong prefix
    [InlineData("YJ-20260615-A7X3K")]        // suffix too short
    [InlineData("YJ-20260615-A7X3K90")]      // suffix too long
    [InlineData("YJ-20260615-A7X3KO")]       // O in suffix (excluded)
    [InlineData("YJ-20260615-a7x3k9")]       // lowercase
    public void From_throws_when_format_invalid(string value)
    {
        var act = () => BookingReference.From(value);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Alphabet_constant_excludes_ambiguous_characters()
    {
        BookingReference.Alphabet.Should().NotContain("O").And.NotContain("0").And.NotContain("I").And.NotContain("1");
        BookingReference.Alphabet.Length.Should().Be(32);
    }

    [Fact]
    public void ToString_returns_the_value()
    {
        var reference = BookingReference.Compose(new DateOnly(2026, 1, 1), "BCDEFG");

        reference.ToString().Should().Be("YJ-20260101-BCDEFG");
    }
}
