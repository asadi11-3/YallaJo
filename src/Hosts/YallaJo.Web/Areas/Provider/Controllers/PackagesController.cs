using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Provider.Facades;
using YallaJo.Web.Areas.Provider.Models.Packages;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Identity;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize(Policy = "Provider")]
public sealed class PackagesController : BaseController
{
    private readonly PackagesFacade _facade;
    private readonly ICurrentUser _currentUser;

    public PackagesController(PackagesFacade facade, ICurrentUser currentUser)
    {
        _facade = facade;
        _currentUser = currentUser;
    }

    // ── GET /provider/packages ────────────────────────────────────────────────────
    [HttpGet("provider/packages")]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {

        var result = await _facade.GetIndexAsync(page, ct);
        return result.Outcome switch
        {
            PackageOutcome.Ok => View(result.Data),
            PackageOutcome.ForceSignOut => RedirectToLogin(),
            PackageOutcome.Forbidden => Status(result.Error),
            _ => Fail(result.Error),
        };
    }

    // ── POST /provider/packages/create ──────────────────────────────────────────────
    [HttpPost("provider/packages/create")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Package.Create)]
    public async Task<IActionResult> Create(CreatePackageFormVm form, CancellationToken ct = default)
    {

        if (!ModelState.IsValid)
            return await ReloadIndex(form, ct);

        var result = await _facade.CreateAsync(form, ct);
        switch (result.Outcome)
        {
            case PackageOutcome.Ok:
                SetSuccess("Package created as a draft. Add inclusions, then submit it for review.");
                return RedirectToAction(nameof(Index));
            case PackageOutcome.ForceSignOut:
                return RedirectToLogin();
            case PackageOutcome.Forbidden:
                SetError(result.Error);
                return RedirectToStatus();
            default:
                if (!ApplyFacadeValidation(result.ValidationErrors))
                    SetError(result.Error ?? "Could not create the package.");
                return await ReloadIndex(form, ct);
        }
    }

    // ── GET /provider/packages/{id} ──────────────────────────────────────────────────
    [HttpGet("provider/packages/{id:guid}")]
    public async Task<IActionResult> Manage(Guid id, CancellationToken ct = default)
    {

        var result = await _facade.GetManageAsync(id, ct);
        return result.Outcome switch
        {
            PackageOutcome.Ok => View(result.Data),
            PackageOutcome.ForceSignOut => RedirectToLogin(),
            PackageOutcome.Forbidden => Status(result.Error),
            _ => Fail(result.Error),
        };
    }

    // ── POST /provider/packages/{id}/inclusions ──────────────────────────────────────
    [HttpPost("provider/packages/{id:guid}/inclusions")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Package.Update)]
    public async Task<IActionResult> AddInclusion(Guid id, string description, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            SetError("Enter an inclusion description.");
            return RedirectToAction(nameof(Manage), new { id });
        }

        var result = await _facade.AddInclusionAsync(id, description, ct);
        return Finish(result, id, "Inclusion added.");
    }

    // ── POST /provider/packages/{id}/submit ──────────────────────────────────────────
    [HttpPost("provider/packages/{id:guid}/submit")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Package.Update)]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct = default)
    {
        var result = await _facade.SubmitAsync(id, ct);
        return Finish(result, id, "Package submitted for review.");
    }

    // ── POST /provider/packages/{id}/delete ──────────────────────────────────────────
    [HttpPost("provider/packages/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Package.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var result = await _facade.DeleteAsync(id, ct);
        if (result.Outcome == PackageOutcome.ForceSignOut) return RedirectToLogin();

        if (result.Outcome == PackageOutcome.Ok)
            SetSuccess("Package deleted.");
        else
            SetError(result.Error ?? "Could not delete the package.");

        return RedirectToAction(nameof(Index));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    private IActionResult Finish(PackageActionResult result, Guid id, string success)
    {
        if (result.Outcome == PackageOutcome.ForceSignOut) return RedirectToLogin();

        SetFlash(result.Outcome == PackageOutcome.Ok, result.Error, success, "The action could not be completed.");
        return RedirectToAction(nameof(Manage), new { id });
    }

    private async Task<IActionResult> ReloadIndex(CreatePackageFormVm form, CancellationToken ct)
    {
        var result = await _facade.GetIndexAsync(1, ct);
        var vm = result is { Outcome: PackageOutcome.Ok, Data: { } data } ? data : new PackagesIndexVm();
        vm.Create = form;
        return View(nameof(Index), vm);
    }

    private bool ApplyFacadeValidation(IReadOnlyDictionary<string, string[]>? errors)
    {
        if (errors is not { Count: > 0 }) return false;
        foreach (var (field, messages) in errors)
            foreach (var message in messages)
                ModelState.AddModelError($"Create.{field}", message);
        return true;
    }

    private IActionResult Fail(string? message)
    {
        SetError(message ?? "Could not load packages.");
        return View(nameof(Index), new PackagesIndexVm());
    }

    private IActionResult Status(string? message)
    {
        SetError(message);
        return RedirectToStatus();
    }

    private IActionResult RedirectToStatus() =>
        RedirectToAction("Status", "Provider", new { area = "Provider" });

}
