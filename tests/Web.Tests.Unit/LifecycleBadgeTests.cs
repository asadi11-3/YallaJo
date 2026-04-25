using FluentAssertions;
using YallaJo.Web.Areas.Admin.Modules.Security.Features.Users.Helpers;

namespace Web.Tests.Unit;

/// <summary>
/// Phase 5D — covers <see cref="LifecycleBadge"/>. Today the helper
/// short-circuits to the binary IsActive mapping; the
/// <c>lifecycleState</c> parameter is accepted but ignored. These
/// tests pin that contract so a future backend extension that lights
/// up granular states does not silently regress today's mapping.
/// </summary>
public sealed class LifecycleBadgeTests
{
    [Fact]
    public void GetCssClass_Active_NoLifecycleState_ReturnsBgSuccess()
    {
        LifecycleBadge.GetCssClass(isActive: true, lifecycleState: null)
            .Should().Be("bg-success");
    }

    [Fact]
    public void GetCssClass_Inactive_NoLifecycleState_ReturnsBgSecondary()
    {
        LifecycleBadge.GetCssClass(isActive: false, lifecycleState: null)
            .Should().Be("bg-secondary");
    }

    [Fact]
    public void GetLabel_Active_ReturnsActive()
    {
        LifecycleBadge.GetLabel(isActive: true).Should().Be("Active");
    }

    [Fact]
    public void GetLabel_Inactive_ReturnsInactive()
    {
        LifecycleBadge.GetLabel(isActive: false).Should().Be("Inactive");
    }

    [Theory]
    [InlineData("Active")]
    [InlineData("Suspended")]
    [InlineData("PendingActivation")]
    [InlineData("PendingPasswordReset")]
    [InlineData("Provisioned")]
    [InlineData("Archived")]
    [InlineData("")]
    [InlineData("   ")]
    public void GetCssClass_TodayIgnores_LifecycleStateInput_ForBackwardCompat(string lifecycleState)
    {
        // Phase 5D — the helper currently ignores lifecycleState. This
        // test pins that "binary today" semantics so a future patch
        // adding granular palette mapping has to update both branches
        // and the assertions in one place.
        LifecycleBadge.GetCssClass(isActive: true, lifecycleState: lifecycleState)
            .Should().Be("bg-success");
        LifecycleBadge.GetCssClass(isActive: false, lifecycleState: lifecycleState)
            .Should().Be("bg-secondary");
    }

    [Fact]
    public void Helper_IsDeterministic_ForSameInputs()
    {
        for (var i = 0; i < 5; i++)
        {
            LifecycleBadge.GetCssClass(true, "Active").Should().Be("bg-success");
            LifecycleBadge.GetLabel(false, "Suspended").Should().Be("Inactive");
        }
    }
}
