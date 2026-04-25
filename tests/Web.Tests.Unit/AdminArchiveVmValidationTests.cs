using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using YallaJo.Web.Areas.Admin.Modules.Security.Features.Users.Lifecycle.ViewModels;

namespace Web.Tests.Unit;

/// <summary>
/// Phase 5C — verifies the DataAnnotations on <c>AdminArchiveVm</c>.
/// The <c>ConfirmText</c> regex enforces typed-ARCHIVE confirmation
/// server-side; the modal's JS-disabled-submit gate is cosmetic.
/// </summary>
public sealed class AdminArchiveVmValidationTests
{
    private static List<ValidationResult> Validate(AdminArchiveVm vm)
    {
        var ctx = new ValidationContext(vm);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(vm, ctx, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void Valid_When_ConfirmText_IsExactly_ARCHIVE()
    {
        Validate(new AdminArchiveVm { ConfirmText = "ARCHIVE" })
            .Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Invalid_When_ConfirmText_IsBlank(string? text)
    {
        var vm = new AdminArchiveVm { ConfirmText = text! };

        Validate(vm).Should().Contain(r =>
            r.MemberNames.Contains(nameof(AdminArchiveVm.ConfirmText)));
    }

    [Theory]
    [InlineData("archive")]
    [InlineData("Archive")]
    [InlineData("ARCHIV")]
    [InlineData("ARCHIVED")]
    [InlineData("ARCHIVE ")]
    [InlineData(" ARCHIVE")]
    [InlineData("DELETE")]
    public void Invalid_When_ConfirmText_DoesNotMatch_ARCHIVE_Exactly(string text)
    {
        var vm = new AdminArchiveVm { ConfirmText = text };

        Validate(vm).Should().Contain(r =>
            r.MemberNames.Contains(nameof(AdminArchiveVm.ConfirmText)));
    }
}
