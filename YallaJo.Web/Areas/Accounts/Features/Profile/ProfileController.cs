using Microsoft.AspNetCore.Mvc;

namespace YallaJo.Web.Areas.Accounts.Features.Profile
{
    public class ProfileController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
