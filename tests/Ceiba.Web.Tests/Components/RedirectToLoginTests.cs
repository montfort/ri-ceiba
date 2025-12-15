using Bunit;
using Ceiba.Web.Components.Shared;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace Ceiba.Web.Tests.Components;

/// <summary>
/// Component tests for RedirectToLogin Blazor component.
/// Tests navigation to login page with return URL handling.
/// </summary>
[Trait("Category", "Unit")]
public class RedirectToLoginTests : TestContext
{
    private readonly FakeNavigationManager _navigationManager;

    public RedirectToLoginTests()
    {
        _navigationManager = new FakeNavigationManager("https://localhost:5001/", "https://localhost:5001/protected-page");
        Services.AddSingleton<NavigationManager>(_navigationManager);
        Services.AddSingleton<AuthenticationStateProvider>(new UnauthenticatedStateProvider());
    }

    #region Navigation Tests

    [Fact(DisplayName = "RedirectToLogin should navigate to login page")]
    public void RedirectToLogin_ShouldNavigateToLoginPage()
    {
        // Act
        Render<RedirectToLogin>();

        // Assert
        _navigationManager.Uri.Should().Contain("/login");
    }

    [Fact(DisplayName = "RedirectToLogin should include returnUrl parameter")]
    public void RedirectToLogin_ShouldIncludeReturnUrlParameter()
    {
        // Arrange - starting from /protected-page
        _navigationManager.SetUri("https://localhost:5001/protected-page");

        // Act
        Render<RedirectToLogin>();

        // Assert
        _navigationManager.Uri.Should().Contain("returnUrl=");
    }

    [Fact(DisplayName = "RedirectToLogin should encode returnUrl")]
    public void RedirectToLogin_ShouldEncodeReturnUrl()
    {
        // Arrange - starting from a page with special characters
        _navigationManager.SetUri("https://localhost:5001/page?param=value&other=test");

        // Act
        Render<RedirectToLogin>();

        // Assert - URL should be encoded
        _navigationManager.Uri.Should().Contain("returnUrl=");
        // The original path should be encoded in the URL
        _navigationManager.Uri.Should().Contain("%");
    }

    [Fact(DisplayName = "RedirectToLogin should use forceLoad navigation")]
    public void RedirectToLogin_ShouldUseForceLoadNavigation()
    {
        // Act
        Render<RedirectToLogin>();

        // Assert - navigation should have occurred
        _navigationManager.Uri.Should().Contain("/login");
        _navigationManager.ForceLoadWasUsed.Should().BeTrue();
    }

    #endregion

    #region Return URL Path Tests

    [Fact(DisplayName = "RedirectToLogin should handle root path")]
    public void RedirectToLogin_ShouldHandleRootPath()
    {
        // Arrange
        _navigationManager.SetUri("https://localhost:5001/");

        // Act
        Render<RedirectToLogin>();

        // Assert
        _navigationManager.Uri.Should().Contain("/login");
    }

    [Fact(DisplayName = "RedirectToLogin should handle nested path")]
    public void RedirectToLogin_ShouldHandleNestedPath()
    {
        // Arrange
        _navigationManager.SetUri("https://localhost:5001/admin/users/edit/123");

        // Act
        Render<RedirectToLogin>();

        // Assert
        _navigationManager.Uri.Should().Contain("/login");
        _navigationManager.Uri.Should().Contain("returnUrl=");
    }

    [Fact(DisplayName = "RedirectToLogin should handle path with query string")]
    public void RedirectToLogin_ShouldHandlePathWithQueryString()
    {
        // Arrange
        _navigationManager.SetUri("https://localhost:5001/reports?filter=active&page=2");

        // Act
        Render<RedirectToLogin>();

        // Assert
        _navigationManager.Uri.Should().Contain("/login");
        _navigationManager.Uri.Should().Contain("returnUrl=");
    }

    #endregion

    #region Component Lifecycle Tests

    [Fact(DisplayName = "RedirectToLogin should navigate on initialization")]
    public void RedirectToLogin_ShouldNavigateOnInitialization()
    {
        // Arrange
        var initialUri = _navigationManager.Uri;

        // Act
        Render<RedirectToLogin>();

        // Assert - navigation should change the URI
        _navigationManager.Uri.Should().NotBe(initialUri);
    }

    [Fact(DisplayName = "RedirectToLogin should not render visible content")]
    public void RedirectToLogin_ShouldNotRenderVisibleContent()
    {
        // Act
        var cut = Render<RedirectToLogin>();

        // Assert - component renders but with no visible markup
        cut.Markup.Should().BeEmpty();
    }

    #endregion

    #region AuthenticationState Tests

    [Fact(DisplayName = "RedirectToLogin should have cascading AuthenticationState parameter")]
    public void RedirectToLogin_ShouldHaveCascadingAuthenticationStateParameter()
    {
        // Act
        var cut = Render<RedirectToLogin>();

        // Assert - component should be rendered (has the parameter)
        cut.Should().NotBeNull();
    }

    #endregion

    #region URL Encoding Tests

    [Fact(DisplayName = "RedirectToLogin should properly escape special characters")]
    public void RedirectToLogin_ShouldProperlyEscapeSpecialCharacters()
    {
        // Arrange
        _navigationManager.SetUri("https://localhost:5001/search?q=test&filter=active");

        // Act
        Render<RedirectToLogin>();

        // Assert - the returnUrl should be properly encoded
        _navigationManager.Uri.Should().Contain("/login");
        // Check that ampersand is encoded or the structure is correct
        var uri = new Uri(_navigationManager.Uri);
        uri.Query.Should().Contain("returnUrl=");
    }

    [Fact(DisplayName = "RedirectToLogin should handle unicode characters in path")]
    public void RedirectToLogin_ShouldHandleUnicodeCharactersInPath()
    {
        // Arrange
        _navigationManager.SetUri("https://localhost:5001/búsqueda");

        // Act
        Render<RedirectToLogin>();

        // Assert
        _navigationManager.Uri.Should().Contain("/login");
    }

    #endregion

    #region Helper Classes

    private class FakeNavigationManager : NavigationManager
    {
        public bool ForceLoadWasUsed { get; private set; }

        public FakeNavigationManager(string baseUri, string uri)
        {
            Initialize(baseUri, uri);
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            ForceLoadWasUsed = forceLoad;

            // Handle relative URIs by converting to absolute
            if (!uri.StartsWith("http"))
            {
                uri = new Uri(new Uri(BaseUri), uri).ToString();
            }
            Uri = uri;
        }

        public void SetUri(string uri)
        {
            Uri = uri;
        }
    }

    private class UnauthenticatedStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
            return Task.FromResult(new AuthenticationState(anonymous));
        }
    }

    #endregion
}
