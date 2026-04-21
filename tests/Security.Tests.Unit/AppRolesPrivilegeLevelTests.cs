using FluentAssertions;
using Security.Contracts.Authorization;

namespace Security.Tests.Unit;

public sealed class AppRolesPrivilegeLevelTests
{
    [Theory]
    [InlineData(AppRoles.Owner, RolePrivilegeLevel.Owner)]
    [InlineData(AppRoles.SuperAdmin, RolePrivilegeLevel.SuperAdmin)]
    [InlineData(AppRoles.Admin, RolePrivilegeLevel.Admin)]
    [InlineData(AppRoles.User, RolePrivilegeLevel.Standard)]
    [InlineData(AppRoles.TourGuide, RolePrivilegeLevel.Standard)]
    [InlineData(AppRoles.Guest, RolePrivilegeLevel.Standard)]
    [InlineData("owner", RolePrivilegeLevel.Owner)]
    [InlineData("SUPERADMIN", RolePrivilegeLevel.SuperAdmin)]
    [InlineData("some-custom-role", RolePrivilegeLevel.Standard)]
    public void GetPrivilegeLevel_MapsRoleNamesCorrectly(string roleName, RolePrivilegeLevel expected)
    {
        AppRoles.GetPrivilegeLevel(roleName).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetPrivilegeLevel_ReturnsNone_ForBlank(string? roleName)
    {
        AppRoles.GetPrivilegeLevel(roleName).Should().Be(RolePrivilegeLevel.None);
    }

    [Fact]
    public void HighestPrivilegeLevel_PicksMaxAcrossMultipleRoles()
    {
        var roles = new[] { AppRoles.User, AppRoles.Admin, AppRoles.TourGuide };

        AppRoles.HighestPrivilegeLevel(roles).Should().Be(RolePrivilegeLevel.Admin);
    }

    [Fact]
    public void HighestPrivilegeLevel_ReturnsNone_ForEmpty()
    {
        AppRoles.HighestPrivilegeLevel(Array.Empty<string>()).Should().Be(RolePrivilegeLevel.None);
    }

    [Fact]
    public void PrivilegeLevels_AreMonotonicallyOrdered()
    {
        // The hierarchy guard depends on strict numeric ordering.
        (RolePrivilegeLevel.Owner > RolePrivilegeLevel.SuperAdmin).Should().BeTrue();
        (RolePrivilegeLevel.SuperAdmin > RolePrivilegeLevel.Admin).Should().BeTrue();
        (RolePrivilegeLevel.Admin > RolePrivilegeLevel.Standard).Should().BeTrue();
        (RolePrivilegeLevel.Standard > RolePrivilegeLevel.None).Should().BeTrue();
    }
}
