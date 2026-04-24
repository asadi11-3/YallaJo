using FluentAssertions;
using YallaJo.Web.Areas.Admin.Modules.Security.Features.AuditLogs;

namespace Web.Tests.Unit;

/// <summary>
/// Phase 5A — verifies <see cref="AuditLogsApiClient.BuildUrl"/>
/// composes the audit-timeline query string deterministically. The
/// URL is the contract between Web and the
/// <c>GET /api/v1/security/audit-logs</c> endpoint, so any drift would
/// silently break the new filter bar.
/// </summary>
public sealed class AuditLogsApiClientUrlTests
{
    [Fact]
    public void BuildUrl_WithOnlyPagination_ShouldOmitAllOptionalParams()
    {
        var url = AuditLogsApiClient.BuildUrl(
            page: 1, pageSize: 50,
            userId: null, actorUserId: null,
            action: null, from: null, to: null);

        url.Should().Be("/api/v1/security/audit-logs?page=1&pageSize=50");
    }

    [Fact]
    public void BuildUrl_WithSubjectUserId_ShouldAppendUserId_AndPreserveBackwardShape()
    {
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var url = AuditLogsApiClient.BuildUrl(
            page: 1, pageSize: 50,
            userId: userId, actorUserId: null,
            action: null, from: null, to: null);

        url.Should().Be(
            "/api/v1/security/audit-logs?page=1&pageSize=50&userId=11111111-1111-1111-1111-111111111111");
    }

    [Fact]
    public void BuildUrl_WithActorUserId_ShouldAppendActorUserId()
    {
        var actorId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var url = AuditLogsApiClient.BuildUrl(
            page: 2, pageSize: 25,
            userId: null, actorUserId: actorId,
            action: null, from: null, to: null);

        url.Should().Contain("actorUserId=22222222-2222-2222-2222-222222222222");
        url.Should().StartWith("/api/v1/security/audit-logs?page=2&pageSize=25");
    }

    [Fact]
    public void BuildUrl_WithAction_ShouldUrlEscapeTheValue()
    {
        var url = AuditLogsApiClient.BuildUrl(
            page: 1, pageSize: 50,
            userId: null, actorUserId: null,
            action: "ADMIN_REASSIGN_ACCOUNT",
            from: null, to: null);

        url.Should().Contain("action=ADMIN_REASSIGN_ACCOUNT");
    }

    [Fact]
    public void BuildUrl_WithBlankAction_ShouldOmitActionParam()
    {
        var url = AuditLogsApiClient.BuildUrl(
            page: 1, pageSize: 50,
            userId: null, actorUserId: null,
            action: "   ",
            from: null, to: null);

        url.Should().NotContain("action=");
    }

    [Fact]
    public void BuildUrl_WithDateRange_ShouldEncodeIso8601_RoundTrippable()
    {
        var from = new DateTime(2026, 4, 1, 12, 30, 0, DateTimeKind.Utc);
        var to   = new DateTime(2026, 4, 30, 23, 59, 59, DateTimeKind.Utc);

        var url = AuditLogsApiClient.BuildUrl(
            page: 1, pageSize: 50,
            userId: null, actorUserId: null,
            action: null,
            from: from, to: to);

        // The "o" round-trip format ends with "Z" for UTC. Uri.EscapeDataString
        // escapes ':' → "%3A".
        url.Should().Contain("from=2026-04-01T12%3A30%3A00.0000000Z");
        url.Should().Contain("to=2026-04-30T23%3A59%3A59.0000000Z");
    }

    [Fact]
    public void BuildUrl_WithEverySupportedFilter_ShouldKeepStableOrder()
    {
        var userId  = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var actorId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var from    = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to      = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);

        var url = AuditLogsApiClient.BuildUrl(
            page: 3, pageSize: 25,
            userId: userId, actorUserId: actorId,
            action: "ADMIN_SUSPEND_USER",
            from: from, to: to);

        // Stable ordering keeps logs/snapshots predictable; verify the
        // sequence rather than just presence.
        var idxUser   = url.IndexOf("userId=", StringComparison.Ordinal);
        var idxActor  = url.IndexOf("actorUserId=", StringComparison.Ordinal);
        var idxAction = url.IndexOf("action=", StringComparison.Ordinal);
        var idxFrom   = url.IndexOf("from=", StringComparison.Ordinal);
        var idxTo     = url.IndexOf("to=", StringComparison.Ordinal);

        idxUser.Should().BeGreaterThan(0);
        idxActor.Should().BeGreaterThan(idxUser);
        idxAction.Should().BeGreaterThan(idxActor);
        idxFrom.Should().BeGreaterThan(idxAction);
        idxTo.Should().BeGreaterThan(idxFrom);
    }
}
