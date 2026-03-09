using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Models;
using YallaJo.Web.Models.Api;
using YallaJo.Web.Services;

namespace YallaJo.Web.Controllers;

/// <summary>
/// Handles browser-based authentication for the MVC admin panel.
/// Posts credentials to the API, receives a JWT, parses claims,
/// and creates a cookie session for the browser.
/// </summary>
public class AuthController(ApiClient api) : Controller
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        // Call the API login endpoint
        var result = await api.PostAsync<LoginResponse>(
            "/api/auth/login",
            new LoginRequest(model.Email, model.Password),
            ct);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Login failed.");
            return View(model);
        }

        // Parse the JWT to extract claims for the cookie session
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result.Data!.AccessToken);

        var claims = new List<Claim>(token.Claims);

        // Ensure we have a Name claim for display
        if (!claims.Any(c => c.Type == ClaimTypes.Name))
        {
            var sub = claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            if (sub is not null)
                claims.Add(new Claim(ClaimTypes.Name, model.Email));
        }

        // Store the JWT so JwtAuthHandler can forward it to API calls
        claims.Add(new Claim("jwt", result.Data.AccessToken));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);

        return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }
}
