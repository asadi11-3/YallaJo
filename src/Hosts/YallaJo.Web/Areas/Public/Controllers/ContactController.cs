using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Areas.Public.Facades;
using YallaJo.Web.Areas.Public.Models.Contact;
using YallaJo.Web.Infrastructure.Mvc;
using YallaJo.Web.Resources;

namespace YallaJo.Web.Areas.Public.Controllers;

[Area("Public")]
[AllowAnonymous]
public sealed class ContactController : BaseController
{
    private readonly ContactFacade _contact;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ContactController(ContactFacade contact, IStringLocalizer<SharedResource> localizer)
    {
        _contact = contact;
        _localizer = localizer;
    }

    [HttpGet("contact")]
    public Task<IActionResult> Index(CancellationToken ct = default) =>
        Task.FromResult<IActionResult>(View(new ContactFormVm()));

    [HttpGet("contact-2")]
    public Task<IActionResult> Index2(CancellationToken ct = default) =>
        Task.FromResult<IActionResult>(View("Index2", new ContactFormVm()));

    [HttpPost("contact")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(ContactFormVm form, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            return View(nameof(Index), form);
        }

        var result = await _contact.SubmitAsync(form, ct);

        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess)
        {
            if (!ApplyValidationErrors(result))
            {
                SetError(result.Error);
            }

            return View(nameof(Index), form);
        }

        SetSuccess(_localizer["Contact.Submit.Success"]);
        return RedirectToAction(nameof(Index));
    }
}
