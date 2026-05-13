using ContentTours.Application.Commands.Shared;
using ContentTours.Domain.Enums;
using FluentAssertions;

namespace ContentTours.Tests.Unit.Ezz;

/// <summary>
/// Behaviour tests for <see cref="ChildFacilitiesParser"/>.
/// Covers D.8 parser requirements: null/empty handling, separators, case-insensitive
/// enum parsing, dedupe, deterministic ordering, and invalid-token reporting.
/// </summary>
public sealed class ChildFacilitiesParserTests
{
    // ── empty input ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParse_NullOrWhitespace_ReturnsEmpty(string? input)
    {
        var ok = ChildFacilitiesParser.TryParse(input, out var facilities, out var unknownToken);

        ok.Should().BeTrue();
        facilities.Should().BeEmpty();
        unknownToken.Should().BeNull();
    }

    // ── valid CSV ─────────────────────────────────────────────────────────────

    [Fact]
    public void TryParse_ValidCsv_ParsesCaseInsensitively()
    {
        var ok = ChildFacilitiesParser.TryParse(
            "stroller,HIGHchair,playarea",
            out var facilities,
            out var unknownToken);

        ok.Should().BeTrue();
        unknownToken.Should().BeNull();
        facilities.Should().Equal(
            ChildFacility.Stroller,
            ChildFacility.HighChair,
            ChildFacility.PlayArea);
    }

    // ── semicolon separator ──────────────────────────────────────────────────

    [Fact]
    public void TryParse_SemicolonSeparated_ParsesSuccessfully()
    {
        var ok = ChildFacilitiesParser.TryParse(
            "Stroller;PlayArea;ChildSeat",
            out var facilities,
            out var unknownToken);

        ok.Should().BeTrue();
        unknownToken.Should().BeNull();
        facilities.Should().Equal(
            ChildFacility.Stroller,
            ChildFacility.PlayArea,
            ChildFacility.ChildSeat);
    }

    // ── duplicate removal ─────────────────────────────────────────────────────

    [Fact]
    public void TryParse_Duplicates_RemovedFromOutput()
    {
        var ok = ChildFacilitiesParser.TryParse(
            "Stroller,stroller,Stroller,PlayArea,playarea",
            out var facilities,
            out var unknownToken);

        ok.Should().BeTrue();
        unknownToken.Should().BeNull();
        facilities.Should().Equal(
            ChildFacility.Stroller,
            ChildFacility.PlayArea);
    }

    // ── deterministic ordering ────────────────────────────────────────────────

    [Fact]
    public void TryParse_OutputOrder_IsDeterministicByEnumValue()
    {
        var ok = ChildFacilitiesParser.TryParse(
            "AirConditioning,Stroller,ChildSeat,PlayArea",
            out var facilities,
            out var unknownToken);

        ok.Should().BeTrue();
        unknownToken.Should().BeNull();
        facilities.Should().Equal(
            ChildFacility.Stroller,
            ChildFacility.PlayArea,
            ChildFacility.ChildSeat,
            ChildFacility.AirConditioning);
    }

    // ── invalid token ─────────────────────────────────────────────────────────

    [Fact]
    public void TryParse_InvalidToken_ReturnsFalseAndUnknownToken()
    {
        var ok = ChildFacilitiesParser.TryParse(
            "Stroller,NotAFacility,PlayArea",
            out var facilities,
            out var unknownToken);

        ok.Should().BeFalse();
        facilities.Should().BeEmpty();
        unknownToken.Should().Be("NotAFacility");
    }
}
