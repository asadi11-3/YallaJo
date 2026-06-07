using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Infrastructure.Authorization;
using PrivacyController = YallaJo.Web.Areas.Accounts.Controllers.PrivacyController;

namespace Web.Tests.Unit;

/// <summary>
/// FE-1D — PrivacyController attribute wiring: authenticated, Preference.Read gates the
/// page + export, Preference.Update gates the destructive delete/cancel POSTs, the POSTs
/// are anti-forgery protected and POST-only, the GET page/export perform no mutation, and
/// the delete-data action accepts the typed confirmation.
/// </summary>
public sealed class PrivacyControllerWiringTests
{
    private static MethodInfo Action(string name) =>
        typeof(PrivacyController).GetMethod(name)
            ?? throw new InvalidOperationException($"Action '{name}' not found on PrivacyController.");

    [Fact]
    public void Controller_IsAuthorized_AndInAccountsArea()
    {
        typeof(PrivacyController).GetCustomAttribute<AuthorizeAttribute>()
            .Should().NotBeNull("privacy controls require an authenticated user");
        typeof(PrivacyController).GetCustomAttribute<AreaAttribute>()!.RouteValue.Should().Be("Accounts");
    }

    [Fact]
    public void Index_RequiresPreferenceRead_GetRoute_NoMutation()
    {
        var m = Action(nameof(PrivacyController.Index));
        m.GetCustomAttribute<RequirePermissionAttribute>()!.Permission.Should().Be(WebPermission.Preference.Read);
        m.GetCustomAttribute<HttpGetAttribute>()!.Template.Should().Be("accounts/privacy");
        m.GetCustomAttribute<HttpPostAttribute>().Should().BeNull("the page is read-only and must not be a POST");
        m.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>()
            .Should().BeNull("a non-mutating GET must not carry an anti-forgery requirement");
    }

    [Fact]
    public void Export_RequiresPreferenceRead_GetRoute()
    {
        var m = Action(nameof(PrivacyController.Export));
        m.GetCustomAttribute<RequirePermissionAttribute>()!.Permission.Should().Be(WebPermission.Preference.Read);
        m.GetCustomAttribute<HttpGetAttribute>()!.Template.Should().Be("accounts/privacy/export");
    }

    [Fact]
    public void DeleteData_RequiresPreferenceUpdate_PostOnly_AntiForgery_AndTypedConfirmation()
    {
        var m = Action(nameof(PrivacyController.DeleteData));
        m.GetCustomAttribute<RequirePermissionAttribute>()!.Permission.Should().Be(WebPermission.Preference.Update);
        m.GetCustomAttribute<HttpPostAttribute>()!.Template.Should().Be("accounts/privacy/delete-data");
        m.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>()
            .Should().NotBeNull("deleting data is destructive and must be anti-forgery protected");
        m.GetParameters().Should().Contain(p => p.Name == "confirmation",
            "the destructive action must receive the typed confirmation value");
    }

    [Fact]
    public void CancelDeletion_RequiresPreferenceUpdate_PostOnly_AntiForgery()
    {
        var m = Action(nameof(PrivacyController.CancelDeletion));
        m.GetCustomAttribute<RequirePermissionAttribute>()!.Permission.Should().Be(WebPermission.Preference.Update);
        m.GetCustomAttribute<HttpPostAttribute>()!.Template.Should().Be("accounts/privacy/cancel-deletion");
        m.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>()
            .Should().NotBeNull("cancel is a state-changing POST and must be anti-forgery protected");
    }
}
