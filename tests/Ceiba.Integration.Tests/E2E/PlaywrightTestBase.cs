using System.Reflection;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;
using Xunit.Sdk;

namespace Ceiba.Integration.Tests.E2E;

/// <summary>
/// Custom attribute to capture test names for tracing.
/// </summary>
public class WithTestNameAttribute : BeforeAfterTestAttribute
{
    public static string CurrentTestName = string.Empty;
    public static string CurrentClassName = string.Empty;

    public override void Before(MethodInfo methodInfo)
    {
        CurrentTestName = methodInfo.Name;
        CurrentClassName = methodInfo.DeclaringType!.Name;
    }

    public override void After(MethodInfo methodInfo)
    {
    }
}

/// <summary>
/// Base class for Playwright E2E tests.
/// Provides common configuration and trace-on-failure support.
/// </summary>
[WithTestName]
[Trait("Category", "E2E")]
public abstract class PlaywrightTestBase : PageTest
{
    protected static string BaseUrl => Environment.GetEnvironmentVariable("BASE_URL") ?? "http://localhost:5000";

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        // Start tracing for debugging failures
        await Context.Tracing.StartAsync(new()
        {
            Title = $"{WithTestNameAttribute.CurrentClassName}.{WithTestNameAttribute.CurrentTestName}",
            Screenshots = true,
            Snapshots = true,
            Sources = true
        });
    }

    public override async Task DisposeAsync()
    {
        // Stop tracing, save only on failure
        var tracePath = !TestOk ? Path.Combine(
            Environment.CurrentDirectory,
            "playwright-traces",
            $"{WithTestNameAttribute.CurrentClassName}.{WithTestNameAttribute.CurrentTestName}.zip"
        ) : null;

        if (tracePath != null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(tracePath)!);
        }

        await Context.Tracing.StopAsync(new()
        {
            Path = tracePath
        });

        await base.DisposeAsync();
    }

    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions()
        {
            BaseURL = BaseUrl,
            ViewportSize = new ViewportSize
            {
                Width = 1280,
                Height = 720
            },
            Locale = "es-MX",
            TimezoneId = "America/Mexico_City"
        };
    }

    /// <summary>
    /// Navigate to a path relative to the base URL.
    /// </summary>
    protected async Task NavigateToAsync(string path)
    {
        await Page.GotoAsync(path);
    }

    /// <summary>
    /// Wait for the page to be fully loaded (network idle).
    /// </summary>
    protected async Task WaitForPageLoadAsync()
    {
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Take a screenshot with a descriptive name.
    /// </summary>
    protected async Task TakeScreenshotAsync(string name)
    {
        var screenshotDir = Path.Combine(Environment.CurrentDirectory, "playwright-screenshots");
        Directory.CreateDirectory(screenshotDir);

        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(screenshotDir, $"{WithTestNameAttribute.CurrentClassName}_{name}.png"),
            FullPage = true
        });
    }
}
