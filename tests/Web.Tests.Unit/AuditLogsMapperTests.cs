using FluentAssertions;
using YallaJo.Web.Areas.Admin.Modules.Security.Features.AuditLogs.Mappers;
using YallaJo.Web.Areas.Admin.Modules.Security.Features.AuditLogs.Responses;

namespace Web.Tests.Unit;

/// <summary>
/// Phase 5A — verifies <c>AuditLogsMapper.ToRowVm</c> projects every
/// Phase 4 field. Without this, a backend payload extension could
/// silently regress to the legacy 4-column row shape.
/// </summary>
public sealed class AuditLogsMapperTests
{
    [Fact]
    public void ToRowVm_ShouldCopy_AllPhase4Fields()
    {
        var dto = new AuditLogItemResponse
        {
            Id           = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            UserId       = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            ActorUserId  = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            Action       = "ADMIN_REASSIGN_ACCOUNT",
            ResourceType = "User",
            ResourceId   = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            IpAddress    = "203.0.113.7",
            Reason       = "left company",
            Metadata     = "{\"oldEmail\":\"old@x.test\"}",
            OccurredAt   = new DateTime(2026, 4, 26, 12, 0, 0, DateTimeKind.Utc),
        };

        var vm = AuditLogsMapper.ToRowVm(dto);

        vm.Id.Should().Be(dto.Id);
        vm.UserId.Should().Be(dto.UserId);
        vm.ActorUserId.Should().Be(dto.ActorUserId);
        vm.Action.Should().Be(dto.Action);
        vm.ResourceType.Should().Be(dto.ResourceType);
        vm.ResourceId.Should().Be(dto.ResourceId);
        vm.IpAddress.Should().Be(dto.IpAddress);
        vm.Reason.Should().Be(dto.Reason);
        vm.Metadata.Should().Be(dto.Metadata);
        vm.OccurredAt.Should().Be(dto.OccurredAt);
    }

    [Fact]
    public void ToRowVm_ShouldKeepNullables_ForLegacyRows()
    {
        // Legacy rows (REGISTER, LOGIN, etc.) leave the new admin
        // columns null. The mapper must propagate that faithfully.
        var dto = new AuditLogItemResponse
        {
            Id           = Guid.NewGuid(),
            UserId       = Guid.NewGuid(),
            ActorUserId  = null,
            Action       = "REGISTER",
            ResourceType = "User",
            ResourceId   = null,
            IpAddress    = null,
            Reason       = null,
            Metadata     = null,
            OccurredAt   = DateTime.UtcNow,
        };

        var vm = AuditLogsMapper.ToRowVm(dto);

        vm.ActorUserId.Should().BeNull();
        vm.Reason.Should().BeNull();
        vm.Metadata.Should().BeNull();
        vm.ResourceId.Should().BeNull();
    }
}
