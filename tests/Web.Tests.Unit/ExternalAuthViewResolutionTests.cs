using System.Diagnostics;
using System.IO;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Web.Tests.Unit;

/// <summary>
/// Regression tests for the feature-folder Razor view layout. The
/// ExternalAuth flow rendered <c>Complete.cshtml</c> with the default view
/// convention, but Complete.cshtml lives under the shared
/// <c>Features/ExternalProviders/</c> folder — MVC could not locate it and
/// threw <see cref="System.InvalidOperationException"/> at runtime.
///
/// <para>
/// These tests spin up a minimal MVC services container wired with the same
/// view-location formats as Program.cs and assert that every Razor asset
/// referenced by ExternalAuthController resolves.
/// </para>
/// </summary>
public sealed class ExternalAuthViewResolutionTests : IDisposable
{
    private readonly IHost _host;
    private readonly IServiceScope _scope;

    public ExternalAuthViewResolutionTests()
    {
        var webRoot = LocateWebProjectRoot();

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<IWebHostEnvironment>(new StubWebHostEnvironment(webRoot));
                // Razor's view engine depends on a DiagnosticListener from DI.
                var listener = new DiagnosticListener("YallaJo.Tests");
                services.AddSingleton(listener);
                services.AddSingleton<DiagnosticSource>(listener);
                services.AddMvcCore()
                    .AddViews()
                    .AddRazorViewEngine(o =>
                    {
                        // Mirror Program.cs exactly.
                        o.AreaViewLocationFormats.Add("~/Areas/{2}/Features/{1}/Views/{0}.cshtml");
                        o.AreaViewLocationFormats.Add("~/Areas/{2}/Features/{1}/Views/Shared/{0}.cshtml");
                        o.AreaViewLocationFormats.Add("~/Areas/{2}/Shared/{0}.cshtml");
                    });
            })
            .Build();

        _scope = _host.Services.CreateScope();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _host.Dispose();
    }

    [Fact]
    public void CompleteView_ResolvesUnderSharedFeatureFolder_ForExternalAuthController()
    {
        var engine = _scope.ServiceProvider.GetRequiredService<IRazorViewEngine>();

        // ExternalAuthController.Callback renders the view via an explicit
        // absolute path — verify that path points at a real file so the
        // "view not found" runtime bug cannot reappear.
        // Must match ExternalAuthController.CompleteViewPath (Areas/Auth/Controllers/ExternalAuthController.cs).
        const string completePath =
            "~/Areas/Auth/Views/ExternalProviders/Complete.cshtml";

        var result = engine.GetView(
            executingFilePath: null,
            viewPath: completePath,
            isMainPage: true);

        result.Success.Should().BeTrue(
            "Complete.cshtml is shipped at this path and ExternalAuthController " +
            "returns it via View(CompleteViewPath, vm).");
    }

    [Fact]
    public void RecaptchaFieldPartial_ResolvesUnderAreaSharedFolder()
    {
        var engine = _scope.ServiceProvider.GetRequiredService<IRazorViewEngine>();

        // Auth views reference this partial as <partial name="_RecaptchaField" />.
        // It lives at Areas/Auth/Shared/ and is reachable through the
        // ~/Areas/{area}/Shared/{0}.cshtml search format registered in Program.cs.
        var result = engine.FindView(
            new Microsoft.AspNetCore.Mvc.ActionContext(
                new Microsoft.AspNetCore.Http.DefaultHttpContext
                {
                    RequestServices = _scope.ServiceProvider,
                },
                new RouteData(new RouteValueDictionary
                {
                    ["area"] = "Auth",
                    ["controller"] = "Login",
                }),
                new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor
                {
                    RouteValues = new Dictionary<string, string?>
                    {
                        ["area"] = "Auth",
                        ["controller"] = "Login",
                    },
                }),
            viewName: "_RecaptchaField",
            isMainPage: false);

        result.Success.Should().BeTrue(
            "partial must resolve through ~/Areas/{area}/Shared/{0}.cshtml search format");
    }

    private static string LocateWebProjectRoot()
    {
        // Walk up from the test binary to the repo root (marked by YallaJo.sln),
        // then down to the Web host. The project lives at src/Hosts/YallaJo.Web
        // per the repo layout — the pre-restructure path (<repo>/YallaJo.Web)
        // does not exist.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "YallaJo.sln")))
            dir = dir.Parent;

        if (dir is null)
            throw new InvalidOperationException("Could not locate repository root from test binary.");

        return Path.Combine(dir.FullName, "src", "Hosts", "YallaJo.Web");
    }

    private sealed class StubWebHostEnvironment : IWebHostEnvironment
    {
        public StubWebHostEnvironment(string contentRoot)
        {
            ContentRootPath = contentRoot;
            ContentRootFileProvider = new PhysicalFileProvider(contentRoot);
            var wwwroot = Path.Combine(contentRoot, "wwwroot");
            WebRootPath = Directory.Exists(wwwroot) ? wwwroot : contentRoot;
            WebRootFileProvider = new PhysicalFileProvider(WebRootPath);
            EnvironmentName = Environments.Development;
            ApplicationName = "YallaJo.Web";
        }

        public string WebRootPath { get; set; }
        public IFileProvider WebRootFileProvider { get; set; }
        public string ApplicationName { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; }
        public string ContentRootPath { get; set; }
        public string EnvironmentName { get; set; }
    }

}
