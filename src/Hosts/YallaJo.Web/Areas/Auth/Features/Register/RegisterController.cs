using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Auth.Features.Register.ViewModels;

namespace YallaJo.Web.Areas.Auth.Features.Register;

[Area("Auth")]
[AllowAnonymous]
public sealed class RegisterController : Controller
{
    private readonly RegisterFacade _facade;

    public RegisterController(RegisterFacade facade) => _facade = facade;

    [HttpGet]
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Sessions", new { area = "Auth" });

        return View(new RegisterVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(RegisterVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var result = await _facade.HandleAsync(vm, ct);

        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] =
                "Registration successful. Please check your email for a verification code.";
            return RedirectToAction("Index", "VerifyEmail",
                new { area = "Auth", email = result.Email });
        }

        if (result.ValidationErrors is not null)
        {
            foreach (var (field, messages) in result.ValidationErrors)
                foreach (var msg in messages)
                    ModelState.AddModelError(field, msg);
            return View(vm);
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "Registration failed.");
        return View(vm);
    }
}
