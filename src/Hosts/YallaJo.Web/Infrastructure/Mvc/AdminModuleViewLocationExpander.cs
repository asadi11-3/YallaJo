using Microsoft.AspNetCore.Mvc.Razor;

namespace YallaJo.Web.Infrastructure.Mvc;

/// <summary>
/// Automatically discovers Admin area module view paths so that the Razor engine
/// can locate views under:
///   Areas/Admin/Modules/{Module}/Features/{Controller}/Views/{View}.cshtml
///
/// This replaces the manual foreach-per-module list in Program.cs.
/// New Admin modules are picked up automatically when their folder is created —
/// no Program.cs change required.
///
/// Registered as a singleton IViewLocationExpander in Program.cs via:
///   o.ViewLocationExpanders.Add(new AdminModuleViewLocationExpander(webRootPath))
/// </summary>
public sealed class AdminModuleViewLocationExpander : IViewLocationExpander
{
    private readonly IReadOnlyList<string> _extraLocations;

    public AdminModuleViewLocationExpander(string contentRootPath)
    {
        var modulesRoot = Path.Combine(contentRootPath, "Areas", "Admin", "Modules");

        if (!Directory.Exists(modulesRoot))
        {
            _extraLocations = [];
            return;
        }

        // Discover module names from the filesystem — no manual list needed.
        var moduleNames = Directory
            .GetDirectories(modulesRoot)
            .Select(Path.GetFileName)
            .Where(n => n is not null)
            .Cast<string>()
            .ToList();

        var locations = new List<string>(moduleNames.Count * 2);
        foreach (var module in moduleNames)
        {
            locations.Add($"~/Areas/Admin/Modules/{module}/Features/{{1}}/Views/{{0}}.cshtml");
            locations.Add($"~/Areas/Admin/Modules/{module}/Features/{{1}}/Views/Shared/{{0}}.cshtml");
        }

        _extraLocations = locations;
    }

    // PopulateValues is called per-request; nothing extra to add.
    public void PopulateValues(ViewLocationExpanderContext context) { }

    public IEnumerable<string> ExpandViewLocations(
        ViewLocationExpanderContext context,
        IEnumerable<string> viewLocations)
    {
        // Only intercept Admin area requests to avoid unnecessary path searches.
        if (!string.Equals(context.AreaName, "Admin", StringComparison.OrdinalIgnoreCase))
            return viewLocations;

        return _extraLocations.Concat(viewLocations);
    }
}
