using ContentTours.Application.Commands.TourSchedule.Common;
using FluentAssertions;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Tests.Unit.Mohammad;

/// <summary>
/// Pure expansion-engine tests for PDF Task-2A B2. The engine is the contract surface
/// that the CreateTourScheduleCommandHandler depends on; pinning it independently
/// catches drift before it reaches the integration layer.
/// </summary>
public sealed class TourScheduleRecurrenceExpanderTests
{
    private static readonly TimeOnly Start = new(9, 0);
    private static readonly TimeOnly End = new(11, 0);
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public void Once_WithSingleCustomDate_EmitsOneCandidate()
    {
        var date = Today.AddDays(3);

        var result = TourScheduleRecurrenceExpander.Expand(
            TourSchedulePattern.Once,
            daysOfWeek: null,
            customDates: new[] { date },
            startTime: Start,
            endTime:   End,
            validFrom: null,
            validTo:   null);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().ContainSingle(c =>
            c.DayOfWeek == (byte)date.DayOfWeek
            && c.StartTime == Start
            && c.EndTime == End);
    }

    [Fact]
    public void Once_WithoutCustomDate_FallsBackToValidFromOrToday()
    {
        var result = TourScheduleRecurrenceExpander.Expand(
            TourSchedulePattern.Once,
            daysOfWeek: null,
            customDates: null,
            startTime: Start,
            endTime:   End,
            validFrom: null,   // → today UTC
            validTo:   null);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCount(1);
        result.Value![0].DayOfWeek.Should().Be((byte)Today.DayOfWeek);
    }

    [Fact]
    public void Daily_AcrossOneWeek_EmitsOneCandidatePerDay()
    {
        // 7 consecutive days = 7 candidates
        var from = Today;
        var to = Today.AddDays(6);

        var result = TourScheduleRecurrenceExpander.Expand(
            TourSchedulePattern.Daily,
            daysOfWeek: null, customDates: null,
            startTime: Start, endTime: End,
            validFrom: from, validTo: to);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Count.Should().Be(7);
    }

    [Fact]
    public void Weekly_OnlyEmitsRequestedDaysOfWeek()
    {
        // Weekly with Mon/Wed/Fri across 14 days → 6 emissions
        var from = Today;
        var to = Today.AddDays(13);
        var requestedDays = new byte[]
        {
            (byte)DayOfWeek.Monday,
            (byte)DayOfWeek.Wednesday,
            (byte)DayOfWeek.Friday,
        };

        var result = TourScheduleRecurrenceExpander.Expand(
            TourSchedulePattern.Weekly,
            daysOfWeek: requestedDays, customDates: null,
            startTime: Start, endTime: End,
            validFrom: from, validTo: to);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().OnlyContain(c => requestedDays.Contains(c.DayOfWeek));
        // 14-day window must contain exactly 2 of each requested weekday
        result.Value!.Count(c => c.DayOfWeek == (byte)DayOfWeek.Monday).Should().Be(2);
        result.Value!.Count(c => c.DayOfWeek == (byte)DayOfWeek.Wednesday).Should().Be(2);
        result.Value!.Count(c => c.DayOfWeek == (byte)DayOfWeek.Friday).Should().Be(2);
    }

    [Fact]
    public void Weekly_WithoutDaysOfWeek_ReturnsPatternParamsInvalid()
    {
        var result = TourScheduleRecurrenceExpander.Expand(
            TourSchedulePattern.Weekly,
            daysOfWeek: null, customDates: null,
            startTime: Start, endTime: End,
            validFrom: Today, validTo: Today.AddDays(7));

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().ContainSingle(e => e.Code == "TourSchedule.PatternParamsInvalid");
    }

    [Fact]
    public void Weekly_WithDayOfWeekOutOfRange_ReturnsInvalidDayOfWeek()
    {
        var result = TourScheduleRecurrenceExpander.Expand(
            TourSchedulePattern.Weekly,
            daysOfWeek: new byte[] { 7 },   // 0..6 valid; 7 invalid
            customDates: null,
            startTime: Start, endTime: End,
            validFrom: Today, validTo: Today.AddDays(7));

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().ContainSingle(e => e.Code == "TourSchedule.InvalidDayOfWeek");
    }

    [Fact]
    public void Custom_WithDateOutsideWindow_ReturnsCustomDateOutOfRange()
    {
        var result = TourScheduleRecurrenceExpander.Expand(
            TourSchedulePattern.Custom,
            daysOfWeek: null,
            customDates: new[] { Today.AddDays(120) },   // outside cap
            startTime: Start, endTime: End,
            validFrom: null, validTo: null);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.UnprocessableEntity);
        result.Errors.Should().ContainSingle(e => e.Code == "TourSchedule.CustomDateOutOfRange");
    }

    [Fact]
    public void Custom_WithoutDates_ReturnsPatternParamsInvalid()
    {
        var result = TourScheduleRecurrenceExpander.Expand(
            TourSchedulePattern.Custom,
            daysOfWeek: null, customDates: null,
            startTime: Start, endTime: End,
            validFrom: null, validTo: null);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().ContainSingle(e => e.Code == "TourSchedule.PatternParamsInvalid");
    }

    [Fact]
    public void OverHardLimit_ReturnsExpansionTooLarge()
    {
        // Custom with 121 dates trips the 120-row hard cap.
        var dates = Enumerable.Range(0, 121).Select(i => Today.AddDays(i % 60)).ToList();

        var result = TourScheduleRecurrenceExpander.Expand(
            TourSchedulePattern.Custom,
            daysOfWeek: null, customDates: dates,
            startTime: Start, endTime: End,
            validFrom: null, validTo: null);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.UnprocessableEntity);
        result.Errors.Should().ContainSingle(e => e.Code == "TourSchedule.ExpansionTooLarge");
    }

    [Fact]
    public void Daily_WithValidFromBeyondCap_ReturnsPatternParamsInvalid()
    {
        // ValidFrom past today+90 — must fail-fast and emit zero rows.
        var result = TourScheduleRecurrenceExpander.Expand(
            TourSchedulePattern.Daily,
            daysOfWeek: null, customDates: null,
            startTime: Start, endTime: End,
            validFrom: Today.AddDays(100), validTo: null);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().ContainSingle(e => e.Code == "TourSchedule.PatternParamsInvalid");
    }

    [Fact]
    public void Weekly_WithValidFromBeyondCap_ReturnsPatternParamsInvalid()
    {
        // Same 90-day window guard for Weekly.
        var result = TourScheduleRecurrenceExpander.Expand(
            TourSchedulePattern.Weekly,
            daysOfWeek: new byte[] { 1, 3, 5 }, customDates: null,
            startTime: Start, endTime: End,
            validFrom: Today.AddDays(100), validTo: Today.AddDays(120));

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().ContainSingle(e => e.Code == "TourSchedule.PatternParamsInvalid");
    }
}
