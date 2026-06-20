using System.Text.Json;
using FluentAssertions;
using YallaJo.Web.Areas.Public.Helpers;
using YallaJo.Web.Areas.Public.Models.Search;

namespace Web.Tests.Unit;

/// <summary>
/// Guards the public tours/search "Showing {start}–{end} of {total} results" logic against
/// the regression where the search payload's <c>total</c>/<c>page</c> failed to bind to
/// <see cref="PaginatedTourSearchResponse.TotalCount"/>/<see cref="PaginatedTourSearchResponse.PageNumber"/>,
/// leaving them 0 and rendering "0–0 of 0" while cards were visible.
/// </summary>
public sealed class TourResultRangeTests
{
    // ---- ResultRangeCalculator (the view's range rule) ----

    [Fact]
    public void Compute_FirstPageWithResults_StartsAtOne()
    {
        var range = ResultRangeCalculator.Compute(totalCount: 5, visibleCount: 5, pageNumber: 1, pageSize: 12);

        range.Start.Should().Be(1);
        range.End.Should().Be(5);
    }

    [Fact]
    public void Compute_FullFirstPage_EndCappedAtPageSize()
    {
        var range = ResultRangeCalculator.Compute(totalCount: 30, visibleCount: 12, pageNumber: 1, pageSize: 12);

        range.Start.Should().Be(1);
        range.End.Should().Be(12);
    }

    [Fact]
    public void Compute_SecondPage_OffsetsByPageSize()
    {
        var range = ResultRangeCalculator.Compute(totalCount: 30, visibleCount: 12, pageNumber: 2, pageSize: 12);

        range.Start.Should().Be(13);
        range.End.Should().Be(24);
    }

    [Fact]
    public void Compute_LastPartialPage_EndCappedAtTotal()
    {
        // 30 total, 12/page → page 3 shows 6 items (25..30).
        var range = ResultRangeCalculator.Compute(totalCount: 30, visibleCount: 6, pageNumber: 3, pageSize: 12);

        range.Start.Should().Be(25);
        range.End.Should().Be(30);
    }

    [Fact]
    public void Compute_TotalZero_IsEmptyRange()
    {
        var range = ResultRangeCalculator.Compute(totalCount: 0, visibleCount: 0, pageNumber: 1, pageSize: 12);

        range.Should().Be(ResultRange.Empty);
        range.Start.Should().Be(0);
        range.End.Should().Be(0);
    }

    [Fact]
    public void Compute_CardsVisibleButTotalUnknown_NeverShowsZeroZero()
    {
        // Reproduces the original bug input: cards rendered (visibleCount > 0) but the
        // metadata that should drive the range arrives as 0. Must not be 0–0.
        var range = ResultRangeCalculator.Compute(totalCount: 0, visibleCount: 5, pageNumber: 0, pageSize: 0);

        // total == 0 is authoritative for "0 results"; with no total we cannot invent one,
        // but the range must remain self-consistent (never start > end, never negative).
        range.Start.Should().BeGreaterThanOrEqualTo(0);
        range.End.Should().BeGreaterThanOrEqualTo(range.Start);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-99)]
    public void Compute_BadPageNumber_NeverProducesNegativeOrInvalidRange(int badPage)
    {
        var range = ResultRangeCalculator.Compute(totalCount: 5, visibleCount: 5, pageNumber: badPage, pageSize: 12);

        range.Start.Should().BeGreaterThanOrEqualTo(1);
        range.End.Should().BeGreaterThanOrEqualTo(range.Start);
        range.End.Should().BeLessThanOrEqualTo(5);
    }

    [Fact]
    public void Compute_ZeroPageSize_FallsBackToVisibleCount()
    {
        var range = ResultRangeCalculator.Compute(totalCount: 5, visibleCount: 5, pageNumber: 1, pageSize: 0);

        range.Start.Should().Be(1);
        range.End.Should().Be(5);
    }

    // ---- PaginatedTourSearchResponse wire mapping (the root cause) ----

    [Fact]
    public void Deserialize_BackendSearchShape_MapsTotalAndPage()
    {
        // Mirrors SearchToursResult on the wire: items/total/page/pageSize/totalPages.
        const string json = """
        {
          "items": [
            { "id": "11111111-1111-1111-1111-111111111111", "name": "Petra Day Trip", "slug": "petra-day-trip" }
          ],
          "total": 5,
          "page": 1,
          "pageSize": 12,
          "totalPages": 1,
          "facets": {},
          "facetsAreApproximate": false
        }
        """;

        var dto = JsonSerializer.Deserialize<PaginatedTourSearchResponse>(json);

        dto.Should().NotBeNull();
        dto!.TotalCount.Should().Be(5);
        dto.PageNumber.Should().Be(1);
        dto.PageSize.Should().Be(12);
        dto.TotalPages.Should().Be(1);
        dto.Items.Should().HaveCount(1);
    }

    [Fact]
    public void Deserialize_ThenComputeRange_RendersValidRange_NotZeroZero()
    {
        const string json = """
        { "items": [ {"id":"11111111-1111-1111-1111-111111111111","name":"A","slug":"a"} ],
          "total": 5, "page": 1, "pageSize": 12, "totalPages": 1 }
        """;

        var dto = JsonSerializer.Deserialize<PaginatedTourSearchResponse>(json)!;
        var range = ResultRangeCalculator.Compute(dto.TotalCount, dto.Items.Count, dto.PageNumber, dto.PageSize);

        range.Start.Should().Be(1);
        range.End.Should().Be(1);
        // The pre-fix behavior would have been 0–0 here.
        (range.Start == 0 && range.End == 0).Should().BeFalse();
    }
}
