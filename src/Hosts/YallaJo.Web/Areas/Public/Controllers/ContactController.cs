using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Public.Facades;
using YallaJo.Web.Areas.Public.Models.Contact;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Public.Controllers;

[Area("Public")]
[AllowAnonymous]
public sealed class ContactController : BaseController
{
    private readonly ContactFacade _contact;

    public ContactController(ContactFacade contact) => _contact = contact;

    [HttpGet("contact")]
    public IActionResult Index() => View(new ContactFormVm());

    [HttpGet("contact-2")]
    public IActionResult Index2() => View("Index2", new ContactFormVm());

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

        SetSuccess("Your message has been sent. Our support team will get back to you.");
        return RedirectToAction(nameof(Index));
    }
}
