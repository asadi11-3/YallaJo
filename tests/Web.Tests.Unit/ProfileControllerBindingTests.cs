using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Accounts.Controllers;
using YallaJo.Web.Areas.Accounts.Models.Profile;


namespace Web.Tests.Unit;

/// <summary>
/// Regression guard for the Profile "Details" update request shape.
/// <para>
/// The Razor "Details" form is rendered from a partial
/// (<c>Views/_UpdateProfileForm.cshtml</c>) whose model is
/// <see cref="UpdateProfileVm"/>. That means input names render FLAT
/// (<c>FirstName</c>, <c>LastName</c>, …). The controller MUST therefore
/// accept a plain <see cref="UpdateProfileVm"/> parameter WITHOUT a
/// <c>[Bind(Prefix)]</c> attribute — binding works because the request
/// shape matches the parameter shape, not because of prefix tricks.
/// </para>
/// </summary>
public sealed class ProfileControllerBindingTests
{
    [Fact]
    public void Update_Action_Should_Accept_Flat_UpdateProfileVm()
    {
        var method = typeof(ProfileController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .SingleOrDefault(m => m.Name == "Update"
                && m.GetParameters().Any(p => p.ParameterType == typeof(UpdateProfileVm)));

        method.Should().NotBeNull("ProfileController.Update(UpdateProfileVm, ...) must exist");

        var param = method!.GetParameters()
            .Single(p => p.ParameterType == typeof(UpdateProfileVm));

        param.GetCustomAttribute<BindAttribute>().Should().BeNull(
            "the request shape should match the parameter shape directly; " +
            "if [Bind(Prefix)] is needed, the form is not flat and the view/partial is wrong");

        param.GetCustomAttribute<FromFormAttribute>().Should().BeNull(
            "default form-body binding is sufficient; no explicit source attribute required");
    }

    [Fact]
    public void Update_Action_Should_Be_AttributeRouted_ToAccountsProfileUpdate()
    {
        var method = typeof(ProfileController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Single(m => m.Name == "Update"
                && m.GetParameters().Any(p => p.ParameterType == typeof(UpdateProfileVm)));

        var post = method.GetCustomAttribute<HttpPostAttribute>();
        post.Should().NotBeNull("Update must be POST-only");
        post!.Template.Should().Be("accounts/profile/update",
            "the form posts to this explicit path; breaking it silently 404s the form");
    }

    [Fact]
    public void Update_Partial_View_Should_Exist_And_Be_Flat_Typed()
    {
        // The partial is what guarantees the form inputs render FLAT (no
        // "Update." prefix), which in turn lets the controller accept a plain
        // UpdateProfileVm. If someone deletes/renames the partial or changes
        // its model, the binding contract breaks.
        var webProjectDir = LocateWebProjectRoot();
        var partialPath = Path.Combine(
            webProjectDir,
            "Areas", "Accounts", "Views", "Profile", "_UpdateProfileForm.cshtml");

        File.Exists(partialPath).Should().BeTrue(
            $"the Details form is rendered from {partialPath}; " +
            "without it the Index view would have to inline the form under Model.Update.*");

        var content = File.ReadAllText(partialPath);
        content.Should().Contain(
            "@model YallaJo.Web.Areas.Accounts.Models.Profile.UpdateProfileVm",
            "the partial must be typed to UpdateProfileVm so inputs render FLAT");
    }

    private static string LocateWebProjectRoot()
    {
        // Walk up looking for src/Hosts/YallaJo.Web (the real project location).
        // The pre-restructure path (<repo>/YallaJo.Web) does not exist.
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 10 && dir is not null; i++)
        {
            var candidate = Path.Combine(dir, "src", "Hosts", "YallaJo.Web");
            if (Directory.Exists(candidate)) return candidate;
            dir = Path.GetDirectoryName(dir);
        }

        throw new InvalidOperationException(
            "Could not locate src/Hosts/YallaJo.Web from test output directory.");
    }
}
