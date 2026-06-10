using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace YallaJo.Web.Infrastructure.Routing;

/// <summary>
/// Convenience helpers for producing encoded IDs from views / controllers without
/// directly resolving <see cref="IIdEncoder"/>.
/// <para>
/// Outbound encoding is OPT-IN: a view explicitly calls <c>Url.EncodeId(...)</c> (or uses the
/// <c>yj-encode-id</c> tag helper). Nothing is rewritten globally, so non-Admin areas are
/// unaffected until they adopt it in their own phase.
/// </para>
/// </summary>
public static class IdEncoderUrlExtensions
{
    /// <summary>Encodes a GUID via the request-scoped <see cref="IIdEncoder"/>.</summary>
    public static string EncodeId(this IUrlHelper url, Guid id)
    {
        ArgumentNullException.ThrowIfNull(url);
        var encoder = url.ActionContext.HttpContext.RequestServices.GetRequiredService<IIdEncoder>();
        return encoder.Encode(id);
    }

    /// <summary>Encodes a nullable GUID; returns <c>null</c> when the input is null.</summary>
    public static string? EncodeId(this IUrlHelper url, Guid? id)
        => id is { } value ? url.EncodeId(value) : null;
}
