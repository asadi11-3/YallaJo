using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace YallaJo.Web.Infrastructure.Routing;

/// <summary>
/// Supplies <see cref="EncodedIdModelBinder"/> for every <see cref="Guid"/> / <see cref="Guid?"/>
/// action parameter and model property.
/// <para>
/// Registered at the FRONT of the binder-provider list so it takes precedence over the
/// framework's default simple-type binder for GUIDs. Because the binder accepts raw GUIDs too,
/// this is a transparent, backward-compatible interception — no behavioural change for callers
/// that still pass plain GUIDs.
/// </para>
/// </summary>
public sealed class EncodedIdModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var modelType = context.Metadata.ModelType;
        var underlying = Nullable.GetUnderlyingType(modelType) ?? modelType;

        if (underlying == typeof(Guid))
        {
            var encoder = (IIdEncoder)context.Services.GetService(typeof(IIdEncoder))!;
            return new EncodedIdModelBinder(encoder);
        }

        return null;
    }
}
