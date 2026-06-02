using FluentAssertions;
using YallaJo.Web.Areas.Admin.Models.Users;


namespace Web.Tests.Unit;

/// <summary>
/// Phase 5D — minimal coverage for <c>UsersMapper.ToRowVm</c>. The
/// mapper is small but un-tested before this slice; the new Users
/// list partials depend on its output, so a regression here would
/// silently break the page.
/// </summary>
public sealed class UsersMapperTests
{
    [Fact]
    public void ToRowVm_ShouldCopy_AllShape_Fields()
    {
        var dto = new UserItemResponse
        {
            Id       = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Email    = "alice@example.test",
            IsActive = true,
            Roles    = new List<string> { "Admin", "TourGuide" },
        };

        var vm = UsersMapper.ToRowVm(dto);

        vm.Id.Should().Be(dto.Id);
        vm.Email.Should().Be("alice@example.test");
        vm.IsActive.Should().BeTrue();
        vm.Roles.Should().BeEquivalentTo(new[] { "Admin", "TourGuide" });
    }

    [Fact]
    public void ToRowVm_ShouldPreserve_EmptyRoles_AsEmptyList_NotNull()
    {
        var dto = new UserItemResponse
        {
            Id       = Guid.NewGuid(),
            Email    = "noroles@example.test",
            IsActive = true,
            Roles    = Array.Empty<string>(),
        };

        var vm = UsersMapper.ToRowVm(dto);

        vm.Roles.Should().NotBeNull();
        vm.Roles.Should().BeEmpty();
    }

    [Fact]
    public void ToRowVm_ShouldFlow_IsActiveFalse_ForInactiveUser()
    {
        var dto = new UserItemResponse
        {
            Id       = Guid.NewGuid(),
            Email    = "deactivated@example.test",
            IsActive = false,
            Roles    = new List<string> { "User" },
        };

        var vm = UsersMapper.ToRowVm(dto);

        vm.IsActive.Should().BeFalse(
            "the lifecycle badge partial relies on this field to render Active vs. Inactive");
    }
}
