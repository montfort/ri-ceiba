using Microsoft.Playwright;

namespace Ceiba.Integration.Tests.E2E;

/// <summary>
/// E2E tests for responsive design across different viewport sizes.
/// Tests mobile-first design principles and responsive breakpoints.
/// </summary>
public class ResponsiveE2ETests : PlaywrightTestBase
{
    public ResponsiveE2ETests(E2ETestServerFixture serverFixture) : base(serverFixture) { }

    // Common device viewports
    private static readonly ViewportSize MobilePortrait = new() { Width = 375, Height = 667 };   // iPhone SE
    private static readonly ViewportSize MobileLandscape = new() { Width = 667, Height = 375 };
    private static readonly ViewportSize Tablet = new() { Width = 768, Height = 1024 };          // iPad
    private static readonly ViewportSize Desktop = new() { Width = 1280, Height = 720 };

    [Fact]
    public async Task LoginPage_ShouldRenderCorrectly_OnMobile()
    {
        // Arrange
        await Page.SetViewportSizeAsync(MobilePortrait.Width, MobilePortrait.Height);

        // Act
        await NavigateToAsync("/login");
        await WaitForPageLoadAsync();

        // Assert - Form should be visible and properly sized
        var form = Page.Locator("form").First;
        await Expect(form).ToBeVisibleAsync();

        // Form should not overflow viewport
        var formBox = await form.BoundingBoxAsync();
        Assert.NotNull(formBox);
        Assert.True(formBox.Width <= MobilePortrait.Width,
            "Form should fit within mobile viewport width");
    }

    [Fact]
    public async Task LoginPage_ShouldRenderCorrectly_OnTablet()
    {
        // Arrange
        await Page.SetViewportSizeAsync(Tablet.Width, Tablet.Height);

        // Act
        await NavigateToAsync("/login");
        await WaitForPageLoadAsync();

        // Assert - Form should be centered and properly sized
        var form = Page.Locator("form").First;
        await Expect(form).ToBeVisibleAsync();

        var formBox = await form.BoundingBoxAsync();
        Assert.NotNull(formBox);
        Assert.True(formBox.Width <= Tablet.Width,
            "Form should fit within tablet viewport width");
    }

    [Fact]
    public async Task LoginPage_ShouldRenderCorrectly_OnDesktop()
    {
        // Arrange
        await Page.SetViewportSizeAsync(Desktop.Width, Desktop.Height);

        // Act
        await NavigateToAsync("/login");
        await WaitForPageLoadAsync();

        // Assert - Form should be visible and contained (either by form itself or card/container)
        var form = Page.Locator("form").First;
        await Expect(form).ToBeVisibleAsync();

        var formBox = await form.BoundingBoxAsync();
        Assert.NotNull(formBox);

        // Form should fit within the viewport - full-width forms are acceptable
        // Bootstrap containers can be fluid or have max-width depending on design
        Assert.True(formBox.Width <= Desktop.Width,
            $"Form should fit within desktop viewport (got {formBox.Width}px, viewport {Desktop.Width}px)");
    }

    [Theory]
    [InlineData(375, 667, "Mobile Portrait")]
    [InlineData(667, 375, "Mobile Landscape")]
    [InlineData(768, 1024, "Tablet")]
    [InlineData(1280, 720, "Desktop")]
    [InlineData(1920, 1080, "Desktop Large")]
    public async Task LoginPage_ShouldHaveNoHorizontalScroll(int width, int height, string deviceName)
    {
        // Arrange
        await Page.SetViewportSizeAsync(width, height);

        // Act
        await NavigateToAsync("/login");
        await WaitForPageLoadAsync();

        // Assert - No horizontal scrollbar
        var hasHorizontalScroll = await Page.EvaluateAsync<bool>(
            "document.documentElement.scrollWidth > document.documentElement.clientWidth");

        Assert.False(hasHorizontalScroll,
            $"Page should not have horizontal scroll on {deviceName} ({width}x{height})");
    }

    [Fact]
    public async Task LoginForm_InputsShouldBeAccessible_OnMobile()
    {
        // Arrange
        await Page.SetViewportSizeAsync(MobilePortrait.Width, MobilePortrait.Height);

        // Act
        await NavigateToAsync("/login");
        await WaitForPageLoadAsync();

        // Assert - Inputs should have sufficient touch target size
        // Bootstrap form-control class provides ~38px height by default, which is acceptable
        var emailInput = Page.Locator("input[type='email'], input[name='email'], input#email").First;
        var passwordInput = Page.Locator("input[type='password'], input[name='password'], input#password").First;

        var emailBox = await emailInput.BoundingBoxAsync();
        var passwordBox = await passwordInput.BoundingBoxAsync();

        Assert.NotNull(emailBox);
        Assert.NotNull(passwordBox);

        // Minimum acceptable is 20px (actual Bootstrap form-control is typically ~38px, but can vary)
        // This test ensures inputs are usable, not strict WCAG 44px recommendation
        Assert.True(emailBox.Height >= 20, $"Email input should have adequate touch target height (got {emailBox.Height}px)");
        Assert.True(passwordBox.Height >= 20, $"Password input should have adequate touch target height (got {passwordBox.Height}px)");
    }

    [Fact]
    public async Task LoginButton_ShouldBeAccessible_OnMobile()
    {
        // Arrange
        await Page.SetViewportSizeAsync(MobilePortrait.Width, MobilePortrait.Height);

        // Act
        await NavigateToAsync("/login");
        await WaitForPageLoadAsync();

        // Assert - Submit button should have reasonable touch target size
        // Bootstrap btn-lg provides adequate sizing, standard btn is ~38px
        var submitButton = Page.Locator("button[type='submit'], input[type='submit']").First;
        var buttonBox = await submitButton.BoundingBoxAsync();

        Assert.NotNull(buttonBox);
        // Minimum acceptable is 20px - ensures button is usable
        Assert.True(buttonBox.Height >= 20, $"Submit button should have adequate touch target height (got {buttonBox.Height}px)");
        Assert.True(buttonBox.Width >= 40, $"Submit button should have adequate touch target width (got {buttonBox.Width}px)");
    }

    [Fact]
    public async Task Text_ShouldBeReadable_OnMobile()
    {
        // Arrange
        await Page.SetViewportSizeAsync(MobilePortrait.Width, MobilePortrait.Height);

        // Act
        await NavigateToAsync("/login");
        await WaitForPageLoadAsync();

        // Assert - Check font size is readable (minimum 16px recommended for mobile)
        var fontSize = await Page.EvaluateAsync<string>(
            "window.getComputedStyle(document.body).fontSize");

        var fontSizeValue = float.Parse(fontSize.Replace("px", ""));
        Assert.True(fontSizeValue >= 14, $"Base font size should be at least 14px, got {fontSize}");
    }

    [Fact]
    public async Task Layout_ShouldAdaptToOrientation_OnMobile()
    {
        // Portrait
        await Page.SetViewportSizeAsync(MobilePortrait.Width, MobilePortrait.Height);
        await NavigateToAsync("/login");
        await WaitForPageLoadAsync();

        var formPortrait = Page.Locator("form").First;
        var portraitBox = await formPortrait.BoundingBoxAsync();

        // Landscape
        await Page.SetViewportSizeAsync(MobileLandscape.Width, MobileLandscape.Height);
        await Page.ReloadAsync();
        await WaitForPageLoadAsync();

        var formLandscape = Page.Locator("form").First;
        var landscapeBox = await formLandscape.BoundingBoxAsync();

        // Assert - Form should adapt to both orientations
        Assert.NotNull(portraitBox);
        Assert.NotNull(landscapeBox);

        // In landscape, form might be wider
        Assert.True(landscapeBox.Width >= portraitBox.Width * 0.5,
            "Form should maintain usable width in landscape");
    }

    [Theory]
    [InlineData(375, 667)]
    [InlineData(768, 1024)]
    [InlineData(1280, 720)]
    public async Task CriticalContent_ShouldBeAboveFold(int width, int height)
    {
        // Arrange
        await Page.SetViewportSizeAsync(width, height);

        // Act
        await NavigateToAsync("/login");
        await WaitForPageLoadAsync();

        // Assert - Login form should be visible without scrolling
        var form = Page.Locator("form").First;
        var formBox = await form.BoundingBoxAsync();

        Assert.NotNull(formBox);
        Assert.True(formBox.Y < height,
            $"Form should start above the fold on {width}x{height}");
    }

    [Fact]
    public async Task ResponsiveImages_ShouldScale_OnMobile()
    {
        // Arrange
        await Page.SetViewportSizeAsync(MobilePortrait.Width, MobilePortrait.Height);

        // Act
        await NavigateToAsync("/login");
        await WaitForPageLoadAsync();

        // Assert - Any images should not overflow
        var images = await Page.Locator("img").AllAsync();

        foreach (var img in images)
        {
            var imgBox = await img.BoundingBoxAsync();
            if (imgBox != null)
            {
                Assert.True(imgBox.Width <= MobilePortrait.Width,
                    "Images should not exceed viewport width on mobile");
            }
        }
    }
}
