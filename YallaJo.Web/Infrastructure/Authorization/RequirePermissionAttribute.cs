using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using YallaJo.Web.Infrastructure.Identity;

namespace YallaJo.Web.Infrastructure.Authorization;

   [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class RequirePermissionAttribute : TypeFilterAttribute
    {
        public RequirePermissionAttribute(string permission)
            : base(typeof(RequirePermissionFilter))
        {
            Permission = permission;
            Arguments  = [permission];
            Order      = int.MinValue; // run before other filters
        }

        public string Permission { get; }
    }
