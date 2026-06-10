using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace YallaJo.Web.Infrastructure.Routing;

/// <summary>
/// Model binder for <see cref="Guid"/> / <see cref="Guid?"/> action parameters that
/// transparently decodes an opaque encoded ID (produced by <see cref="IIdEncoder.Encode"/>)
/// OR a plain GUID string (backward compatibility).
/// <para>
/// Because <see cref="IIdEncoder.TryDecode"/> accepts both forms, the 119 existing
/// <c>Guid id</c> action signatures across the app do not need to change — old links that
/// still carry raw GUIDs keep resolving, and new Admin links carrying encoded tokens resolve
/// to the same GUID.
/// </para>
/// </summary>
public sealed class EncodedIdModelBinder : IModelBinder
{
    private readonly IIdEncoder _encoder;

    public EncodedIdModelBinder(IIdEncoder encoder) => _encoder = encoder;

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var valueResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueResult == ValueProviderResult.None)
            return Task.CompletedTask; // leave unbound (optional Guid? stays null; required Guid validated later)

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueResult);

        var raw = valueResult.FirstValue;
        var isNullable = Nullable.GetUnderlyingType(bindingContext.ModelType) is not null;

        if (string.IsNullOrWhiteSpace(raw))
        {
            // Empty value: valid null for Guid?, otherwise leave for required-field validation.
            if (isNullable)
                bindingContext.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }

        if (_encoder.TryDecode(raw, out var id))
        {
            bindingContext.Result = ModelBindingResult.Success(id);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(
                bindingContext.ModelName,
                "The supplied identifier is not valid.");
        }

        return Task.CompletedTask;
    }
}
