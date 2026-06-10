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

    /// <summary>
    /// Contact form. <paramref name="subject"/> allows other pages (e.g. package
    /// inquiries, Phase 8.3) to prefill the subject line via ?subject=.
    /// </summary>
    [HttpGet("contact")]
    public IActionResult Index(string? subject = null)
    {
        var trimmed = subject?.Trim();
        if (trimmed is { Length: > 200 })
        {
            trimmed = trimmed[..200];
        }

        return View(new ContactFormVm { Subject = trimmed ?? string.Empty });
    }

    [HttpPost("contact")]
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
