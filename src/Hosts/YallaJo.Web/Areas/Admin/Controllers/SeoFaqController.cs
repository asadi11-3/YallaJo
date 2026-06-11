// <copyright file="SeoFaqController.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace YallaJo.Web.Areas.Admin.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.SeoFaq;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Resources;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.FaqItem.Read)]
public sealed class SeoFaqController : BaseController
{
    private readonly SeoFaqFacade _facade;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public SeoFaqController(SeoFaqFacade facade, IStringLocalizer<SharedResource> localizer)
    {
        this._facade = facade;
        this._localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] FaqFilterRequest request, CancellationToken ct)
    {
        this.ViewData["AdminNav"] = "SeoFaq";
        var result = await this._facade.GetIndexAsync(request, ct);
        if (this.GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess || result.Data is null)
        {
            this.SetError(result.Error);
            return this.View(new SeoFaqVm());
        }

        return this.View(result.Data);
    }

    [HttpPost("admin/seo/faq/create")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.FaqItem.Create)]
    public async Task<IActionResult> Create(CreateFaqItemFormVm form, CancellationToken ct)
    {
        if (!this.ModelState.IsValid)
        {
            this.SetError(this._localizer["Admin.SeoFaq.Flash.CreateFieldsRequired"].Value);
            return this.RedirectToAction(nameof(this.Index), new { entityType = form.EntityType });
        }

        var result = await this._facade.CreateAsync(form, ct);
        if (this.GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        this.SetFlash(result, this._localizer["Admin.SeoFaq.Flash.Created"].Value, this._localizer["Admin.SeoFaq.Flash.CreateFailed"].Value);
        return this.RedirectToAction(nameof(this.Index), new { entityType = form.EntityType });
    }

    [HttpPost("admin/seo/faq/update")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.FaqItem.Update)]
    public async Task<IActionResult> Update(UpdateFaqItemFormVm form, CancellationToken ct)
    {
        if (!this.ModelState.IsValid)
        {
            this.SetError(this._localizer["Admin.SeoFaq.Flash.UpdateFieldsRequired"].Value);
            return this.RedirectToAction(nameof(this.Index));
        }

        var result = await this._facade.UpdateAsync(form, ct);
        if (this.GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        this.SetFlash(result, this._localizer["Admin.SeoFaq.Flash.Updated"].Value, this._localizer["Admin.SeoFaq.Flash.UpdateFailed"].Value);
        return this.RedirectToAction(nameof(this.Index));
    }

    [HttpPost("admin/seo/faq/delete")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.FaqItem.Delete)]
    public async Task<IActionResult> Delete([FromForm] Guid id, CancellationToken ct)
    {
        var result = await this._facade.DeleteAsync(id, ct);
        if (this.GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        this.SetFlash(result, this._localizer["Admin.SeoFaq.Flash.Deleted"].Value, this._localizer["Admin.SeoFaq.Flash.DeleteFailed"].Value);
        return this.RedirectToAction(nameof(this.Index));
    }

    // ── §8.10: batch reorder FAQ items within one entity's list ─────────────────
    [HttpPost("admin/seo/faq/reorder")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.FaqItem.Update)]
    public async Task<IActionResult> Reorder(
        SeoEntityType entityType, Guid entityId, List<Guid> ids, List<int> sortOrders, CancellationToken ct)
    {
        if (ids is null || ids.Count == 0 || sortOrders is null || ids.Count != sortOrders.Count)
        {
            this.SetError(this._localizer["Admin.SeoFaq.Flash.ReorderInvalid"].Value);
            return this.RedirectToAction(nameof(this.Index), new { entityType, entityId });
        }

        var items = ids
            .Select((id, idx) => new ReorderFaqItemApi(id, sortOrders[idx]))
            .ToList();

        var result = await this._facade.ReorderAsync(entityType, entityId, items, ct);
        if (this.GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        this.SetFlash(result, this._localizer["Admin.SeoFaq.Flash.Reordered"].Value, this._localizer["Admin.SeoFaq.Flash.ReorderFailed"].Value);
        return this.RedirectToAction(nameof(this.Index), new { entityType, entityId });
    }
}
