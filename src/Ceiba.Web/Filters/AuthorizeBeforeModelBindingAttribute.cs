using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Ceiba.Web.Filters;

/// <summary>
/// Authorization filter that executes BEFORE model binding and validation.
/// This ensures that authentication is checked before processing the request body,
/// following OWASP security best practices (avoid information disclosure).
/// </summary>
public class AuthorizeBeforeModelBindingAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[]? _roles;

    public AuthorizeBeforeModelBindingAttribute(params string[] roles)
    {
        _roles = roles.Length > 0 ? roles : null;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        // Check if user is authenticated
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // If roles are specified, check if user has any of them
        if (_roles != null && _roles.Length > 0)
        {
            var hasRole = _roles.Any(role => user.IsInRole(role));
            if (!hasRole)
            {
                context.Result = new ForbidResult();
                return;
            }
        }
    }
}
