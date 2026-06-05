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

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.FaqItem.Read)]
public sealed class SeoFaqController : BaseController
{
    private readonly SeoFaqFacade _facade;

    public SeoFaqController(SeoFaqFacade facade) => this._facade = facade;

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
            this.SetError("Please provide a valid entity, question, and answer.");
            return this.RedirectToAction(nameof(this.Index), new { entityType = form.EntityType });
        }

        var result = await this._facade.CreateAsync(form, ct);
        if (this.GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        this.SetFlash(result, "FAQ item created.", "Could not create the FAQ item.");
        return this.RedirectToAction(nameof(this.Index), new { entityType = form.EntityType });
    }

    [HttpPost("admin/seo/faq/update")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.FaqItem.Update)]
    public async Task<IActionResult> Update(UpdateFaqItemFormVm form, CancellationToken ct)
    {
        if (!this.ModelState.IsValid)
        {
            this.SetError("Please provide a valid question and answer.");
            return this.RedirectToAction(nameof(this.Index));
        }

        var result = await this._facade.UpdateAsync(form, ct);
        if (this.GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        this.SetFlash(result, "FAQ item updated.", "Could not update the FAQ item.");
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

        this.SetFlash(result, "FAQ item deleted.", "Could not delete the FAQ item.");
        return this.RedirectToAction(nameof(this.Index));
    }
}
