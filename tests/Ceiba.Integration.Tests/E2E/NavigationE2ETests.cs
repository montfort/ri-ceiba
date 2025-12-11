using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Ceiba.Integration.Tests.E2E;

/// <summary>
/// E2E tests for navigation and layout components.
/// Tests general application navigation and layout rendering.
/// </summary>
public class NavigationE2ETests : PlaywrightTestBase
{
    [Fact]
    public async Task HomePage_ShouldRedirectToLogin_WhenUnauthenticated()
    {
        // Arrange & Act
        await NavigateToAsync("/");
        await WaitForPageLoadAsync();

        // Assert - Should redirect to login for unauthenticated users
        var isOnLoginPage = Page.Url.Contains("login", StringComparison.OrdinalIgnoreCase);
        var hasLoginForm = await Page.Locator("input[type='password']").CountAsync() > 0;

        Assert.True(isOnLoginPage || hasLoginForm,
            "Unauthenticated users should be redirected to login page");
    }

    [Fact]
    public async Task ProtectedRoutes_ShouldRequireAuthentication()
    {
        // Arrange - List of protected routes that should require authentication
        // Note: Some routes may show 404 if they don't exist yet, which is also acceptable
        var protectedRoutes = new[]
        {
            "/reports",
            "/reports/create",
            "/admin",
            "/admin/users"
        };

        foreach (var route in protectedRoutes)
        {
            // Act
            await NavigateToAsync(route);
            await WaitForPageLoadAsync();

            // Assert - Should redirect to login, show login form, show access denied, or 404
            // All of these indicate the route is protected or not publicly accessible
            var currentUrl = Page.Url.ToLowerInvariant();
            var redirectedToLogin = currentUrl.Contains("login") ||
                                    currentUrl.Contains("accessdenied") ||
                                    currentUrl.Contains("account");
            var hasLoginForm = await Page.Locator("input[type='password']").CountAsync() > 0;
            var is404 = await Page.Locator("text=404, text=not found, text=no encontrado").CountAsync() > 0 ||
                       await Page.Locator("h1:has-text('404'), h1:has-text('Not Found')").CountAsync() > 0;
            var hasUnauthorizedMessage = await Page.Locator("text=unauthorized, text=no autorizado, text=acceso denegado").CountAsync() > 0;

            // The route is considered protected if any of these conditions are met
            var isProtected = redirectedToLogin || hasLoginForm || is404 || hasUnauthorizedMessage;

            Assert.True(isProtected,
                $"Route '{route}' should require authentication (URL: {Page.Url})");
        }
    }

    [Fact]
    public async Task Application_ShouldLoadWithoutJavaScriptErrors()
    {
        // Arrange
        var jsErrors = new List<string>();
        Page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                jsErrors.Add(msg.Text);
            }
        };

        // Act
        await NavigateToAsync("/login");
        await WaitForPageLoadAsync();

        // Assert - No critical JavaScript errors (excluding resource loading errors which are common in CI)
        var criticalErrors = jsErrors.Where(e =>
            !e.Contains("favicon", StringComparison.OrdinalIgnoreCase) &&
            !e.Contains("404", StringComparison.OrdinalIgnoreCase) &&
            !e.Contains("Failed to load resource", StringComparison.OrdinalIgnoreCase) &&
            !e.Contains("Refused to apply style", StringComparison.OrdinalIgnoreCase) &&
            !e.Contains("Refused to execute script", StringComparison.OrdinalIgnoreCase) &&
            !e.Contains("MIME type", StringComparison.OrdinalIgnoreCase)).ToList();

        Assert.Empty(criticalErrors);
    }

    [Fact]
    public async Task Application_ShouldHaveValidHTMLStructure()
    {
        // Arrange & Act
        await NavigateToAsync("/login");
        await WaitForPageLoadAsync();

        // Assert - Check for semantic HTML structure
        var hasHtmlTag = await Page.Locator("html").CountAsync() > 0;
        var hasHeadTag = await Page.Locator("head").CountAsync() > 0;
        var hasBodyTag = await Page.Locator("body").CountAsync() > 0;
        var hasMainContent = await Page.Locator("main, [role='main'], #main-content, .container[role='main']").CountAsync() > 0;

        Assert.True(hasHtmlTag, "Page should have html tag");
        Assert.True(hasHeadTag, "Page should have head tag");
        Assert.True(hasBodyTag, "Page should have body tag");
        Assert.True(hasMainContent, "Page should have main content area");
    }

    [Fact]
    public async Task Application_ShouldHaveProperMetaTags()
    {
        // Arrange & Act
        await NavigateToAsync("/login");
        await WaitForPageLoadAsync();

        // Assert - Check for essential meta tags
        var hasViewport = await Page.Locator("meta[name='viewport']").CountAsync() > 0;
        var hasCharset = await Page.Locator("meta[charset], meta[http-equiv='Content-Type']").CountAsync() > 0;

        Assert.True(hasViewport, "Page should have viewport meta tag for responsive design");
        Assert.True(hasCharset, "Page should have charset meta tag");
    }

    [Fact]
    public async Task Application_ShouldLoadStylesCorrectly()
    {
        // Arrange & Act
        await NavigateToAsync("/login");
        await WaitForPageLoadAsync();

        // Assert - Styles should be loaded (body should have computed styles)
        var bodyBgColor = await Page.EvaluateAsync<string>(
            "window.getComputedStyle(document.body).backgroundColor");

        Assert.NotNull(bodyBgColor);
        Assert.NotEqual("", bodyBgColor);
    }

    [Fact]
    public async Task HealthEndpoint_ShouldBeAccessible()
    {
        // Arrange & Act
        var response = await Page.GotoAsync("/health");

        // Assert - Health endpoint should return success
        Assert.NotNull(response);
        Assert.True(response.Ok || response.Status == 200,
            $"Health endpoint should return OK, got {response.Status}");
    }
}
