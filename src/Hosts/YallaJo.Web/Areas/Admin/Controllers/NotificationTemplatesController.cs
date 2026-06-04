using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.NotificationTemplates;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.NotificationTemplate.Read)]
public sealed class NotificationTemplatesController : BaseController
{
    private readonly NotificationTemplatesFacade _facade;

    public NotificationTemplatesController(NotificationTemplatesFacade facade) => _facade = facade;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["AdminNav"] = "NotificationTemplates";
        var result = await _facade.GetIndexAsync(ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new NotificationTemplatesListVm());
        }

        return View(result.Data);
    }

    [HttpGet("admin/notification-templates/create")]
    [RequirePermission(WebPermission.NotificationTemplate.Create)]
    public IActionResult Create()
    {
        ViewData["AdminNav"] = "NotificationTemplates";
        return View("Edit", new NotificationTemplateEditVm { IsNew = true, LanguageCode = "en" });
    }

    [HttpGet("admin/notification-templates/{id:guid}/edit")]
    [RequirePermission(WebPermission.NotificationTemplate.Update)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        ViewData["AdminNav"] = "NotificationTemplates";
        var result = await _facade.GetEditAsync(id, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    [HttpPost("admin/notification-templates")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.NotificationTemplate.Create)]
    public async Task<IActionResult> Create([FromForm] CreateTemplateRequest req, CancellationToken ct)
    {
        ViewData["AdminNav"] = "NotificationTemplates";

        if (string.IsNullOrWhiteSpace(req.Type)
            || string.IsNullOrWhiteSpace(req.Channel)
            || string.IsNullOrWhiteSpace(req.LanguageCode)
            || string.IsNullOrWhiteSpace(req.Title)
            || string.IsNullOrWhiteSpace(req.Body))
        {
            SetError("Type, channel, language, title and body are all required.");
            return View("Edit", new NotificationTemplateEditVm
            {
                IsNew = true,
                Type = req.Type,
                Channel = req.Channel,
                LanguageCode = string.IsNullOrWhiteSpace(req.LanguageCode) ? "en" : req.LanguageCode,
                Title = req.Title,
                Body = req.Body,
                HtmlBody = req.HtmlBody,
            });
        }

        var result = await _facade.CreateAsync(req, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, "Notification template created.", "Could not create the notification template.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("admin/notification-templates/{id:guid}/update")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.NotificationTemplate.Update)]
    public async Task<IActionResult> Update(Guid id, [FromForm] UpdateTemplateRequest req, CancellationToken ct)
    {
        var result = await _facade.UpdateAsync(id, req, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, "Notification template updated.", "Could not update the notification template.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("admin/notification-templates/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.NotificationTemplate.Delete)]
    public async Task<IActionResult> Delete(Guid id, string? rowVersion, CancellationToken ct)
    {
        var result = await _facade.DeleteAsync(id, rowVersion, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, "Notification template deleted.", "Could not delete the notification template.");
        return RedirectToAction(nameof(Index));
    }
}
