using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Infrastructure.Authorization;
using SupportController = YallaJo.Web.Areas.Accounts.Controllers.SupportController;

namespace Web.Tests.Unit;

/// <summary>
/// FE-1C — Accounts SupportController attribute wiring: the controller must require an
/// authenticated user, every action must carry the right permission gate, the mutation
/// POSTs (reply / close) must be anti-forgery protected and POST-only, and the routes
/// must match /accounts/support[/{id}[/messages|/close]].
/// </summary>
public sealed class SupportControllerWiringTests
{
    private static MethodInfo Action(string name) =>
        typeof(SupportController).GetMethod(name)
            ?? throw new InvalidOperationException($"Action '{name}' not found on SupportController.");

    [Fact]
    public void Controller_IsAuthorized_AndInAccountsArea()
    {
        typeof(SupportController).GetCustomAttribute<AuthorizeAttribute>()
            .Should().NotBeNull("support pages require an authenticated user");

        typeof(SupportController).GetCustomAttribute<AreaAttribute>()!
            .RouteValue.Should().Be("Accounts");
    }

    [Fact]
    public void Index_RequiresSupportRead_AndGetRoute()
    {
        var m = Action(nameof(SupportController.Index));
        m.GetCustomAttribute<RequirePermissionAttribute>()!.Permission.Should().Be(WebPermission.SupportTicket.Read);
        m.GetCustomAttribute<HttpGetAttribute>()!.Template.Should().Be("accounts/support");
    }

    [Fact]
    public void Details_RequiresSupportRead_AndGetRouteWithId()
    {
        var m = Action(nameof(SupportController.Details));
        m.GetCustomAttribute<RequirePermissionAttribute>()!.Permission.Should().Be(WebPermission.SupportTicket.Read);
        m.GetCustomAttribute<HttpGetAttribute>()!.Template.Should().Be("accounts/support/{id:guid}");
    }

    [Fact]
    public void Reply_RequiresSupportRead_PostOnly_AndAntiForgery()
    {
        var m = Action(nameof(SupportController.Reply));
        m.GetCustomAttribute<RequirePermissionAttribute>()!.Permission.Should().Be(WebPermission.SupportTicket.Read);
        m.GetCustomAttribute<HttpPostAttribute>()!.Template.Should().Be("accounts/support/{id:guid}/messages");
        m.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>()
            .Should().NotBeNull("reply is a state-changing POST and must be anti-forgery protected");
    }

    [Fact]
    public void Close_RequiresSupportClose_PostOnly_AndAntiForgery()
    {
        var m = Action(nameof(SupportController.Close));
        m.GetCustomAttribute<RequirePermissionAttribute>()!.Permission.Should().Be(WebPermission.SupportTicket.Close);
        m.GetCustomAttribute<HttpPostAttribute>()!.Template.Should().Be("accounts/support/{id:guid}/close");
        m.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>()
            .Should().NotBeNull("close is a destructive POST and must be anti-forgery protected");
    }

    [Fact]
    public void Close_AcceptsRowVersion_Parameter()
    {
        Action(nameof(SupportController.Close)).GetParameters()
            .Should().Contain(p => p.Name == "rowVersion",
                "the close action must receive the RowVersion echoed back from the detail VM");
    }

    [Fact]
    public void Controller_HasNoCreateAction_CreationReusesContactForm()
    {
        // FE-1C intentionally does NOT add an in-Accounts create form — users open new
        // tickets via the existing /contact page.
        typeof(SupportController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(x => x.Name)
            .Should().NotContain(new[] { "Create", "Open", "New" });
    }
}
