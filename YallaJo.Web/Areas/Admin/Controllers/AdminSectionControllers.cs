using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Models;

namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public abstract class AdminSectionControllerBase : Controller
{
    protected IActionResult Section(AdminSectionPageViewModel model)
    {
        ViewData["Title"] = model.Title;
        return View("~/Areas/Admin/Views/Shared/SectionPlaceholder.cshtml", model);
    }
}

public sealed class CategoriesController : AdminSectionControllerBase
{
    public IActionResult Index() => Section(new AdminSectionPageViewModel(
        "Categories",
        "Browse and manage the content taxonomy used across the admin experience.",
        "Content",
        "bi bi-tags",
        "primary",
        ["Add and organize category groups.", "Review assignments before wiring live API data."]));
}

public sealed class LanguagesController : AdminSectionControllerBase
{
    public IActionResult Index() => Section(new AdminSectionPageViewModel(
        "Languages",
        "Manage supported locales and translation readiness from a dedicated admin page.",
        "Localization",
        "bi bi-translate",
        "success",
        ["Track enabled languages for publishing.", "Prepare this page for Content Core language APIs."]));
}

public sealed class TranslationsController : AdminSectionControllerBase
{
    public IActionResult Index() => Section(new AdminSectionPageViewModel(
        "Translations",
        "Review translation workflows and keep the admin route available for upcoming tooling.",
        "Localization",
        "bi bi-chat-square-text",
        "info",
        ["Queue review and approval work here.", "Connect translation jobs once the UI workflow is implemented."]));
}

public sealed class AttachmentsController : AdminSectionControllerBase
{
    public IActionResult Index() => Section(new AdminSectionPageViewModel(
        "Attachments",
        "Open the media management section without hitting a dead route from the dashboard.",
        "Media",
        "bi bi-paperclip",
        "warning",
        ["List uploaded files and ownership metadata.", "Add upload and moderation actions when the screen is wired up."]));
}

public sealed class UsersController : AdminSectionControllerBase
{
    public IActionResult Index() => Section(new AdminSectionPageViewModel(
        "Users",
        "Access the user administration surface directly from the sidebar.",
        "Security",
        "bi bi-people",
        "info",
        ["Review account state and moderation controls.", "Hook this page to security user queries next."]));
}

public sealed class RolesController : AdminSectionControllerBase
{
    public IActionResult Index() => Section(new AdminSectionPageViewModel(
        "Roles",
        "Keep role management navigation functional while the full admin workflow is built out.",
        "Security",
        "bi bi-shield-lock",
        "secondary",
        ["Inspect role definitions and permission coverage.", "Wire role creation and updates to the security API."]));
}

public sealed class AuditLogsController : AdminSectionControllerBase
{
    public IActionResult Index() => Section(new AdminSectionPageViewModel(
        "Audit Logs",
        "Provide a reachable audit log entry point instead of a 404 from admin navigation.",
        "Security",
        "bi bi-journal-text",
        "dark",
        ["Surface security and moderation history here.", "Add filters and export actions after backend wiring."]));
}

public sealed class ProfileController : AdminSectionControllerBase
{
    public IActionResult Index() => Section(new AdminSectionPageViewModel(
        "My Profile",
        "Open the signed-in admin profile page from the sidebar without leaving the authenticated flow.",
        "Account",
        "bi bi-person-circle",
        "primary",
        ["Review profile data and account preferences.", "Connect profile editing once the account UI is implemented."]));
}
