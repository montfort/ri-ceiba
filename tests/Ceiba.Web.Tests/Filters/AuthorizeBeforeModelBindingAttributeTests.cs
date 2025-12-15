using System.Security.Claims;
using Ceiba.Web.Filters;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace Ceiba.Web.Tests.Filters;

/// <summary>
/// Unit tests for AuthorizeBeforeModelBindingAttribute.
/// Tests authorization filter that executes BEFORE model binding.
/// OWASP security best practice tests.
/// </summary>
[Trait("Category", "Unit")]
public class AuthorizeBeforeModelBindingAttributeTests
{
    private AuthorizationFilterContext CreateContext(ClaimsPrincipal? user = null)
    {
        var httpContext = new DefaultHttpContext();
        if (user != null)
        {
            httpContext.User = user;
        }

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        return new AuthorizationFilterContext(
            actionContext,
            new List<IFilterMetadata>());
    }

    private ClaimsPrincipal CreateAuthenticatedUser(params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, "testuser@example.com")
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    private ClaimsPrincipal CreateUnauthenticatedUser()
    {
        var identity = new ClaimsIdentity(); // No authentication type = unauthenticated
        return new ClaimsPrincipal(identity);
    }

    #region Constructor Tests

    [Fact(DisplayName = "Constructor should create attribute with no roles")]
    public void Constructor_NoRoles_CreatesAttribute()
    {
        // Act
        var attribute = new AuthorizeBeforeModelBindingAttribute();

        // Assert
        attribute.Should().NotBeNull();
    }

    [Fact(DisplayName = "Constructor should create attribute with single role")]
    public void Constructor_SingleRole_CreatesAttribute()
    {
        // Act
        var attribute = new AuthorizeBeforeModelBindingAttribute("ADMIN");

        // Assert
        attribute.Should().NotBeNull();
    }

    [Fact(DisplayName = "Constructor should create attribute with multiple roles")]
    public void Constructor_MultipleRoles_CreatesAttribute()
    {
        // Act
        var attribute = new AuthorizeBeforeModelBindingAttribute("ADMIN", "REVISOR", "CREADOR");

        // Assert
        attribute.Should().NotBeNull();
    }

    [Fact(DisplayName = "Constructor should accept empty roles array")]
    public void Constructor_EmptyRoles_CreatesAttribute()
    {
        // Act
        var attribute = new AuthorizeBeforeModelBindingAttribute(Array.Empty<string>());

        // Assert
        attribute.Should().NotBeNull();
    }

    #endregion

    #region OnAuthorization - Unauthenticated User Tests

    [Fact(DisplayName = "OnAuthorization should return Unauthorized for unauthenticated user")]
    public void OnAuthorization_UnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var attribute = new AuthorizeBeforeModelBindingAttribute();
        var context = CreateContext(CreateUnauthenticatedUser());

        // Act
        attribute.OnAuthorization(context);

        // Assert
        context.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact(DisplayName = "OnAuthorization should return Unauthorized when user identity is null")]
    public void OnAuthorization_NullIdentity_ReturnsUnauthorized()
    {
        // Arrange
        var attribute = new AuthorizeBeforeModelBindingAttribute();
        var context = CreateContext();

        // Act
        attribute.OnAuthorization(context);

        // Assert
        context.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact(DisplayName = "OnAuthorization should return Unauthorized with roles when unauthenticated")]
    public void OnAuthorization_UnauthenticatedWithRoles_ReturnsUnauthorized()
    {
        // Arrange
        var attribute = new AuthorizeBeforeModelBindingAttribute("ADMIN");
        var context = CreateContext(CreateUnauthenticatedUser());

        // Act
        attribute.OnAuthorization(context);

        // Assert
        context.Result.Should().BeOfType<UnauthorizedResult>();
    }

    #endregion

    #region OnAuthorization - Authenticated User Without Roles Tests

    [Fact(DisplayName = "OnAuthorization should allow authenticated user when no roles required")]
    public void OnAuthorization_AuthenticatedNoRolesRequired_AllowsAccess()
    {
        // Arrange
        var attribute = new AuthorizeBeforeModelBindingAttribute();
        var context = CreateContext(CreateAuthenticatedUser());

        // Act
        attribute.OnAuthorization(context);

        // Assert
        context.Result.Should().BeNull();
    }

    [Fact(DisplayName = "OnAuthorization should allow authenticated user with roles when no roles required")]
    public void OnAuthorization_AuthenticatedWithRolesNoRolesRequired_AllowsAccess()
    {
        // Arrange
        var attribute = new AuthorizeBeforeModelBindingAttribute();
        var context = CreateContext(CreateAuthenticatedUser("ADMIN", "REVISOR"));

        // Act
        attribute.OnAuthorization(context);

        // Assert
        context.Result.Should().BeNull();
    }

    #endregion

    #region OnAuthorization - Role Authorization Tests

    [Fact(DisplayName = "OnAuthorization should allow user with required role")]
    public void OnAuthorization_UserHasRequiredRole_AllowsAccess()
    {
        // Arrange
        var attribute = new AuthorizeBeforeModelBindingAttribute("ADMIN");
        var context = CreateContext(CreateAuthenticatedUser("ADMIN"));

        // Act
        attribute.OnAuthorization(context);

        // Assert
        context.Result.Should().BeNull();
    }

    [Fact(DisplayName = "OnAuthorization should allow user with any of the required roles")]
    public void OnAuthorization_UserHasAnyRequiredRole_AllowsAccess()
    {
        // Arrange
        var attribute = new AuthorizeBeforeModelBindingAttribute("ADMIN", "REVISOR");
        var context = CreateContext(CreateAuthenticatedUser("REVISOR"));

        // Act
        attribute.OnAuthorization(context);

        // Assert
        context.Result.Should().BeNull();
    }

    [Fact(DisplayName = "OnAuthorization should return Forbidden when user lacks required role")]
    public void OnAuthorization_UserLacksRequiredRole_ReturnsForbidden()
    {
        // Arrange
        var attribute = new AuthorizeBeforeModelBindingAttribute("ADMIN");
        var context = CreateContext(CreateAuthenticatedUser("CREADOR"));

        // Act
        attribute.OnAuthorization(context);

        // Assert
        context.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact(DisplayName = "OnAuthorization should return Forbidden when user lacks all required roles")]
    public void OnAuthorization_UserLacksAllRequiredRoles_ReturnsForbidden()
    {
        // Arrange
        var attribute = new AuthorizeBeforeModelBindingAttribute("ADMIN", "REVISOR");
        var context = CreateContext(CreateAuthenticatedUser("CREADOR"));

        // Act
        attribute.OnAuthorization(context);

        // Assert
        context.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact(DisplayName = "OnAuthorization should allow user with multiple roles when one matches")]
    public void OnAuthorization_UserHasMultipleRolesOneMatches_AllowsAccess()
    {
        // Arrange
        var attribute = new AuthorizeBeforeModelBindingAttribute("ADMIN");
        var context = CreateContext(CreateAuthenticatedUser("CREADOR", "ADMIN", "REVISOR"));

        // Act
        attribute.OnAuthorization(context);

        // Assert
        context.Result.Should().BeNull();
    }

    #endregion

    #region Role-Specific Tests (CREADOR, REVISOR, ADMIN)

    [Fact(DisplayName = "OnAuthorization should allow CREADOR role access")]
    public void OnAuthorization_CreadorRole_AllowsAccess()
    {
        // Arrange
        var attribute = new AuthorizeBeforeModelBindingAttribute("CREADOR");
        var context = CreateContext(CreateAuthenticatedUser("CREADOR"));

        // Act
        attribute.OnAuthorization(context);

        // Assert
        context.Result.Should().BeNull();
    }

    [Fact(DisplayName = "OnAuthorization should allow REVISOR role access")]
    public void OnAuthorization_RevisorRole_AllowsAccess()
    {
        // Arrange
        var attribute = new AuthorizeBeforeModelBindingAttribute("REVISOR");
        var context = CreateContext(CreateAuthenticatedUser("REVISOR"));

        // Act
        attribute.OnAuthorization(context);

        // Assert
        context.Result.Should().BeNull();
    }

    [Fact(DisplayName = "OnAuthorization should allow ADMIN role access")]
    public void OnAuthorization_AdminRole_AllowsAccess()
    {
        // Arrange
        var attribute = new AuthorizeBeforeModelBindingAttribute("ADMIN");
        var context = CreateContext(CreateAuthenticatedUser("ADMIN"));

        // Act
        attribute.OnAuthorization(context);

        // Assert
        context.Result.Should().BeNull();
    }

    [Fact(DisplayName = "OnAuthorization should deny CREADOR when ADMIN required")]
    public void OnAuthorization_CreadorWhenAdminRequired_ReturnsForbidden()
    {
        // Arrange
        var attribute = new AuthorizeBeforeModelBindingAttribute("ADMIN");
        var context = CreateContext(CreateAuthenticatedUser("CREADOR"));

        // Act
        attribute.OnAuthorization(context);

        // Assert
        context.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact(DisplayName = "OnAuthorization should deny REVISOR when ADMIN required")]
    public void OnAuthorization_RevisorWhenAdminRequired_ReturnsForbidden()
    {
        // Arrange
        var attribute = new AuthorizeBeforeModelBindingAttribute("ADMIN");
        var context = CreateContext(CreateAuthenticatedUser("REVISOR"));

        // Act
        attribute.OnAuthorization(context);

        // Assert
        context.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact(DisplayName = "OnAuthorization should allow REVISOR or ADMIN access")]
    public void OnAuthorization_RevisorOrAdmin_AllowsAccess()
    {
        // Arrange
        var attribute = new AuthorizeBeforeModelBindingAttribute("REVISOR", "ADMIN");
        var context = CreateContext(CreateAuthenticatedUser("REVISOR"));

        // Act
        attribute.OnAuthorization(context);

        // Assert
        context.Result.Should().BeNull();
    }

    #endregion

    #region Edge Cases Tests

    [Fact(DisplayName = "OnAuthorization should be case-sensitive for roles")]
    public void OnAuthorization_CaseSensitiveRoles_ReturnsForbidden()
    {
        // Arrange
        var attribute = new AuthorizeBeforeModelBindingAttribute("ADMIN");
        var context = CreateContext(CreateAuthenticatedUser("admin")); // lowercase

        // Act
        attribute.OnAuthorization(context);

        // Assert
        // Role checking is typically case-sensitive in ASP.NET
        context.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact(DisplayName = "OnAuthorization should handle user with no roles")]
    public void OnAuthorization_UserWithNoRoles_ReturnsForbiddenWhenRolesRequired()
    {
        // Arrange
        var attribute = new AuthorizeBeforeModelBindingAttribute("ADMIN");
        var context = CreateContext(CreateAuthenticatedUser()); // No roles

        // Act
        attribute.OnAuthorization(context);

        // Assert
        context.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact(DisplayName = "OnAuthorization should allow user with no roles when no roles required")]
    public void OnAuthorization_UserWithNoRoles_AllowsWhenNoRolesRequired()
    {
        // Arrange
        var attribute = new AuthorizeBeforeModelBindingAttribute();
        var context = CreateContext(CreateAuthenticatedUser()); // No roles

        // Act
        attribute.OnAuthorization(context);

        // Assert
        context.Result.Should().BeNull();
    }

    [Fact(DisplayName = "Attribute should implement IAuthorizationFilter")]
    public void Attribute_ImplementsIAuthorizationFilter()
    {
        // Arrange & Act
        var attribute = new AuthorizeBeforeModelBindingAttribute();

        // Assert
        attribute.Should().BeAssignableTo<IAuthorizationFilter>();
    }

    [Fact(DisplayName = "Attribute should be an Attribute")]
    public void Attribute_IsAttribute()
    {
        // Arrange & Act
        var attribute = new AuthorizeBeforeModelBindingAttribute();

        // Assert
        attribute.Should().BeAssignableTo<Attribute>();
    }

    #endregion

    #region Security Tests

    [Fact(DisplayName = "OnAuthorization should check authentication before role check")]
    public void OnAuthorization_AuthenticationCheckedFirst()
    {
        // Arrange - User with correct role but not authenticated
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, "ADMIN")
        };
        var identity = new ClaimsIdentity(claims); // No auth type = unauthenticated
        var user = new ClaimsPrincipal(identity);

        var attribute = new AuthorizeBeforeModelBindingAttribute("ADMIN");
        var context = CreateContext(user);

        // Act
        attribute.OnAuthorization(context);

        // Assert - Should return Unauthorized, not check roles
        context.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact(DisplayName = "OnAuthorization should prevent information disclosure")]
    public void OnAuthorization_PreventsInformationDisclosure()
    {
        // Arrange - Unauthenticated user should get 401, not 403
        var attribute = new AuthorizeBeforeModelBindingAttribute("ADMIN");
        var context = CreateContext(CreateUnauthenticatedUser());

        // Act
        attribute.OnAuthorization(context);

        // Assert - 401 Unauthorized, not 403 Forbidden
        // This prevents attackers from knowing if the endpoint exists
        context.Result.Should().BeOfType<UnauthorizedResult>();
    }

    #endregion
}
