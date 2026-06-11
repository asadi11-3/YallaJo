using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Provider.Facades;
using YallaJo.Web.Areas.Provider.Models;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize]
public sealed class ProviderController : BaseController
{
    private readonly ProviderFacade _facade;

    public ProviderController(ProviderFacade facade) => _facade = facade;

    // GET /provider/status
    [HttpGet("provider/status")]
    [RequirePermission(WebPermission.ProviderApplication.Read)]
    public async Task<IActionResult> Status(CancellationToken ct)
    {
        var result = await _facade.GetStatusAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new ProviderStatusVm { HasApplication = false });
        }

        return View(result.Data);
    }

    // GET /provider/apply  (optional ?type= deep-link pre-selects the provider type, e.g. "Become a guide")
    [HttpGet("provider/apply")]
    [RequirePermission(WebPermission.ProviderApplication.Register)]
    public IActionResult Apply(string? type = null)
    {
        var options = ProviderMapper.TypeOptions();

        // Honor a ?type= query param only when it matches a known provider-type value (case-insensitive).
        var preset = string.IsNullOrWhiteSpace(type)
            ? string.Empty
            : options.FirstOrDefault(o => string.Equals(o.Value, type.Trim(), StringComparison.OrdinalIgnoreCase))?.Value
              ?? string.Empty;

        return View(new ProviderApplyVm { Type = preset, TypeOptions = options });
    }

    // POST /provider/apply
    [HttpPost("provider/apply")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.ProviderApplication.Register)]
    public async Task<IActionResult> Apply(ProviderApplyVm vm, CancellationToken ct)
    {
        vm = RehydrateOptions(vm);
        if (!ModelState.IsValid) return View(vm);

        var result = await _facade.RegisterAsync(vm, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
        {
            SetSuccess(L["Provider.Flash.ApplicationStarted"]);
            return RedirectToAction(nameof(Status));
        }

        if (ApplyValidationErrors(result)) return View(vm);

        ModelState.AddModelError(string.Empty, result.Error ?? L["Provider.Flash.CouldNotStartApplication"].Value);
        return View(vm);
    }

    // POST /provider/submit
    [HttpPost("provider/submit")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.ProviderApplication.Submit)]
    public async Task<IActionResult> Submit(CancellationToken ct)
    {
        var result = await _facade.SubmitAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, L["Provider.Flash.ApplicationSubmitted"], L["Provider.Flash.CouldNotSubmitApplication"].Value);
        return RedirectToAction(nameof(Status));
    }

    // POST /provider/reapply  (reapply after rejection, then PRG back to status)
    [HttpPost("provider/reapply")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.ProviderApplication.Update)]
    public async Task<IActionResult> Reapply(CancellationToken ct)
    {
        var result = await _facade.ReapplyAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result,
            L["Provider.Flash.Reapplied"],
            L["Provider.Flash.CouldNotReapply"].Value);
        return RedirectToAction(nameof(Status));
    }

    // POST /provider/apply/documents/upload  (application-flow multipart upload, then PRG back to status)
    // Scoped under /provider/apply/* so it does not collide with the standalone
    // ProviderDocumentsController's POST /provider/documents/upload (different permission family).
    [HttpPost("provider/apply/documents/upload")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.ProviderApplication.Create)]
    public async Task<IActionResult> UploadDocument(ProviderDocumentUploadVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            // Surface the first model error as a flash; the status page re-renders the form.
            var firstError = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));
            SetError(firstError ?? L["Provider.Flash.FixUploadForm"].Value);
            return RedirectToAction(nameof(Status));
        }

        var result = await _facade.UploadDocumentAsync(vm, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsValidationError)
            SetError(result.ValidationErrors!.SelectMany(kvp => kvp.Value).FirstOrDefault()
                     ?? L["Provider.Flash.DocumentRejected"].Value);
        else
            SetFlash(result, L["Provider.Flash.DocumentUploaded"], L["Provider.Flash.CouldNotUploadDocument"].Value);

        return RedirectToAction(nameof(Status));
    }

    // POST /provider/apply/documents/replace  (PUT semantics via POST; application-flow replace, then PRG back to status)
    // Scoped under /provider/apply/* to keep the application-document route family distinct from the
    // standalone ProviderDocumentsController (/provider/documents/{id}/replace, ProviderDocument.Update).
    [HttpPost("provider/apply/documents/replace")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.ProviderApplication.Update)]
    public async Task<IActionResult> ReplaceDocument(ReplaceProviderDocumentVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));
            SetError(firstError ?? L["Provider.Flash.FixReplaceForm"].Value);
            return RedirectToAction(nameof(Status));
        }

        var result = await _facade.ReplaceDocumentAsync(vm, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsValidationError)
            SetError(result.ValidationErrors!.SelectMany(kvp => kvp.Value).FirstOrDefault()
                     ?? L["Provider.Flash.ReplacementRejected"].Value);
        else
            SetFlash(result, L["Provider.Flash.DocumentReplaced"], L["Provider.Flash.CouldNotReplaceDocument"].Value);

        return RedirectToAction(nameof(Status));
    }

    private static ProviderApplyVm RehydrateOptions(ProviderApplyVm vm)
    {
        // Type options are not posted back; re-supply them so the form re-renders correctly.
        return new ProviderApplyVm
        {
            Type         = vm.Type,
            BusinessName = vm.BusinessName,
            ContactEmail = vm.ContactEmail,
            ContactPhone = vm.ContactPhone,
            Address      = vm.Address,
            Description  = vm.Description,
            TypeOptions  = ProviderMapper.TypeOptions(),
        };
    }
}
