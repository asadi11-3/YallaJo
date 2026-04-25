using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using YallaJo.Web.Areas.Admin.Modules.Security.Features.Users.Lifecycle.ViewModels;

namespace Web.Tests.Unit;

/// <summary>
/// Phase 5C — verifies the DataAnnotations on <c>AdminReassignAccountVm</c>,
/// in particular the new <see cref="AdminReassignAccountVm.IUnderstand"/>
/// rule that mirrors the modal's required confirmation checkbox so a
/// user bypassing the JS gate still cannot post a reassignment without
/// acknowledging it.
/// </summary>
public sealed class AdminReassignAccountVmValidationTests
{
    private static List<ValidationResult> Validate(AdminReassignAccountVm vm)
    {
        var ctx = new ValidationContext(vm);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(vm, ctx, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void Valid_WhenEmailValid_AndIUnderstandTrue_AndReasonNull()
    {
        var vm = new AdminReassignAccountVm
        {
            NewEmail    = "new@example.test",
            Reason      = null,
            IUnderstand = true,
        };

        Validate(vm).Should().BeEmpty();
    }

    [Fact]
    public void Invalid_WhenIUnderstand_NotChecked()
    {
        var vm = new AdminReassignAccountVm
        {
            NewEmail    = "new@example.test",
            IUnderstand = false,
        };

        Validate(vm).Should().ContainSingle()
            .Which.MemberNames.Should().Contain(nameof(AdminReassignAccountVm.IUnderstand));
    }

    [Fact]
    public void Invalid_WhenEmail_Missing()
    {
        var vm = new AdminReassignAccountVm
        {
            NewEmail    = string.Empty,
            IUnderstand = true,
        };

        Validate(vm).Should().Contain(r =>
            r.MemberNames.Contains(nameof(AdminReassignAccountVm.NewEmail)));
    }

    [Fact]
    public void Invalid_WhenEmail_Malformed()
    {
        var vm = new AdminReassignAccountVm
        {
            NewEmail    = "not-an-email",
            IUnderstand = true,
        };

        Validate(vm).Should().Contain(r =>
            r.MemberNames.Contains(nameof(AdminReassignAccountVm.NewEmail)));
    }

    [Fact]
    public void Invalid_WhenEmail_LongerThan320()
    {
        var local = new string('a', 320 - "@x.test".Length + 1); // produces 321-char address
        var vm = new AdminReassignAccountVm
        {
            NewEmail    = local + "@x.test",
            IUnderstand = true,
        };

        Validate(vm).Should().Contain(r =>
            r.MemberNames.Contains(nameof(AdminReassignAccountVm.NewEmail)));
    }

    [Fact]
    public void Invalid_WhenReason_LongerThan500()
    {
        var vm = new AdminReassignAccountVm
        {
            NewEmail    = "new@example.test",
            Reason      = new string('x', 501),
            IUnderstand = true,
        };

        Validate(vm).Should().Contain(r =>
            r.MemberNames.Contains(nameof(AdminReassignAccountVm.Reason)));
    }
}
