using YallaJo.Web.Areas.Business.Shared;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Business.Controllers;

/// <summary>
/// Shared base for Business-area controllers. Hosts the sidebar wiring that was
/// previously duplicated across all six controllers (D-19).
/// </summary>
public abstract class BusinessControllerBase : BaseController
{
    protected void SetSidebar(string nav, Guid? businessId = null)
    {
        ViewData["BusinessNav"] = nav;
        ViewBag.Sidebar = new BusinessSidebarVm
        {
            DisplayName = User.Identity?.Name ?? "Business",
            BusinessId = businessId
        };
    }
}
