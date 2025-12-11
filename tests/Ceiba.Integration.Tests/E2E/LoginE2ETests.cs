using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Ceiba.Integration.Tests.E2E;

/// <summary>
/// E2E tests for the Login page and authentication flow.
/// Tests US1-AC1: Login page accessible, secure authentication.
/// </summary>
public class LoginE2ETests : PlaywrightTestBase
{
    [Fact]
    public async Task LoginPage_ShouldBeAccessible()
    {
        // Arrange & Act
        await NavigateToAsync("/login");
        await WaitForPageLoadAsync();

        // Assert - Page should load successfully
        await Expect(Page).ToHaveTitleAsync(new Regex(".*Ceiba.*|.*Login.*|.*Iniciar.*", RegexOptions.IgnoreCase));
    }

    [Fact]
    public async Task LoginPage_ShouldDisplayLoginForm()
    {
        // Arrange
        await NavigateToAsync("/login");
        await WaitForPageLoadAsync();

        // Assert - Login form elements should be visible
        var emailInput = Page.Locator("input[type='email'], input[name='email'], input#email");
        var passwordInput = Page.Locator("input[type='password'], input[name='password'], input#password");
        var submitButton = Page.Locator("button[type='submit'], input[type='submit']");

        await Expect(emailInput).ToBeVisibleAsync();
        await Expect(passwordInput).ToBeVisibleAsync();
        await Expect(submitButton).ToBeVisibleAsync();
    }

    [Fact]
    public async Task LoginPage_ShouldShowValidationErrors_WhenEmptySubmit()
    {
        // Arrange
        await NavigateToAsync("/login");
        await WaitForPageLoadAsync();

        // Act - Submit empty form
        var submitButton = Page.Locator("button[type='submit'], input[type='submit']").First;
        await submitButton.ClickAsync();

        // Assert - Should show validation errors or remain on login page
        // (HTML5 validation or server-side validation)
        await Expect(Page).ToHaveURLAsync(new Regex(".*/login.*", RegexOptions.IgnoreCase));
    }

    [Fact]
    public async Task LoginPage_ShouldShowError_WhenInvalidCredentials()
    {
        // Arrange
        await NavigateToAsync("/login");
        await WaitForPageLoadAsync();

        // Act - Submit with invalid credentials
        await Page.FillAsync("input[type='email'], input[name='email'], input#email", "invalid@test.com");
        await Page.FillAsync("input[type='password'], input[name='password'], input#password", "WrongPassword123!");

        var submitButton = Page.Locator("button[type='submit'], input[type='submit']").First;
        await submitButton.ClickAsync();

        await WaitForPageLoadAsync();

        // Assert - Should show error message or remain on login page
        var hasError = await Page.Locator(".validation-summary-errors, .alert-danger, [role='alert']").CountAsync() > 0;
        var stillOnLogin = Page.Url.Contains("login", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasError || stillOnLogin, "Should show error or remain on login page");
    }

    [Fact]
    public async Task LoginPage_ShouldHaveProperAccessibility()
    {
        // Arrange
        await NavigateToAsync("/login");
        await WaitForPageLoadAsync();

        // Assert - Check for accessibility features
        // 1. Form should have proper labels
        var emailLabel = Page.Locator("label[for='email'], label:has-text('Email'), label:has-text('Correo')");
        var passwordLabel = Page.Locator("label[for='password'], label:has-text('Password'), label:has-text('Contraseña')");

        // 2. Skip link should exist (WCAG 2.4.1)
        var skipLink = Page.Locator("a[href='#main-content'], a.skip-link, [class*='skip']");

        // At least one accessibility feature should be present
        var hasEmailLabel = await emailLabel.CountAsync() > 0;
        var hasPasswordLabel = await passwordLabel.CountAsync() > 0;
        var hasSkipLink = await skipLink.CountAsync() > 0;

        Assert.True(hasEmailLabel || hasPasswordLabel || hasSkipLink,
            "Page should have proper accessibility features (labels or skip link)");
    }

    [Fact]
    public async Task LoginPage_ShouldBeKeyboardNavigable()
    {
        // Arrange
        await NavigateToAsync("/login");
        await WaitForPageLoadAsync();

        // Act - Navigate with Tab key
        await Page.Keyboard.PressAsync("Tab");
        await Page.Keyboard.PressAsync("Tab");
        await Page.Keyboard.PressAsync("Tab");

        // Assert - Focus should move through form elements
        var focusedElement = await Page.EvaluateAsync<string>("document.activeElement?.tagName");
        Assert.NotNull(focusedElement);
        Assert.NotEqual("BODY", focusedElement);
    }
}
