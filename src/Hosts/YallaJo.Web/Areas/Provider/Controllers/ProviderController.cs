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

    // GET /provider/apply
    [HttpGet("provider/apply")]
    [RequirePermission(WebPermission.ProviderApplication.Register)]
    public IActionResult Apply()
        => View(new ProviderApplyVm { TypeOptions = ProviderMapper.TypeOptions() });

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
            SetSuccess("Provider application started. Submit it for review when you're ready.");
            return RedirectToAction(nameof(Status));
        }

        if (ApplyValidationErrors(result)) return View(vm);

        ModelState.AddModelError(string.Empty, result.Error ?? "Could not start your provider application.");
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

        SetFlash(result, "Application submitted for review.", "Could not submit your application.");
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
            "Your application has been reopened as a draft. Update it and submit again when ready.",
            "Could not reapply for your provider application.");
        return RedirectToAction(nameof(Status));
    }

    // POST /provider/documents/upload  (multipart file upload, then PRG back to status)
    [HttpPost("provider/documents/upload")]
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
            SetError(firstError ?? "Please correct the document upload form and try again.");
            return RedirectToAction(nameof(Status));
        }

        var result = await _facade.UploadDocumentAsync(vm, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsValidationError)
            SetError(result.ValidationErrors!.SelectMany(kvp => kvp.Value).FirstOrDefault()
                     ?? "The uploaded document was rejected. Check the file type and size.");
        else
            SetFlash(result, "Document uploaded.", "Could not upload the document.");

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
