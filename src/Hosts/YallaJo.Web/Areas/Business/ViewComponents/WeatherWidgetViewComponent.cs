using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Business.Facades;

namespace YallaJo.Web.Areas.Business.ViewComponents;

/// <summary>
/// Reusable Place-contextual weather widget (guide §9.5 Pattern C). Invoke with
/// <c>@await Component.InvokeAsync("WeatherWidget", new { placeId })</c>. Renders nothing
/// useful (an empty card) when weather is unavailable — never throws.
/// </summary>
public sealed class WeatherWidgetViewComponent : ViewComponent
{
    private readonly BusinessWeatherFacade _facade;

    public WeatherWidgetViewComponent(BusinessWeatherFacade facade) => _facade = facade;

    public async Task<IViewComponentResult> InvokeAsync(Guid placeId)
    {
        var vm = await _facade.GetForPlaceAsync(placeId, HttpContext.RequestAborted);
        return View(vm);
    }
}
