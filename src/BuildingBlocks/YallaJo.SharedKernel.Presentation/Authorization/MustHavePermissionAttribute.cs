using Microsoft.AspNetCore.Authorization;

namespace YallaJo.SharedKernel.Presentation.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class MustHavePermissionAttribute : AuthorizeAttribute
{
    public string Feature { get; }
    public string Action { get; }

    public MustHavePermissionAttribute(string feature, string action)
        : base(PermissionPolicyNames.Build(feature, action))
    {
        Feature = feature;
        Action = action;
    }
}
