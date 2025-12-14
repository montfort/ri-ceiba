using Microsoft.AspNetCore.Components;

namespace Ceiba.Web.Components.Shared;

/// <summary>
/// Live region for screen reader announcements (WCAG 4.1.3).
/// Uses an intentional singleton pattern to maintain a single active announcer
/// for the entire application's accessibility needs.
/// </summary>
#pragma warning disable S2696 // Instance members should not write to "static" fields
public partial class LiveAnnouncer : IDisposable
{
    [Parameter]
    public string Politeness { get; set; } = "polite";

    private string _message = string.Empty;

    // Static instance for global access - intentional singleton pattern for accessibility announcements
    private static LiveAnnouncer? _instance;

    protected override void OnInitialized()
    {
        _instance = this;
    }

    public static void Announce(string message)
    {
        if (_instance != null)
        {
            _instance._message = string.Empty;
            _instance.StateHasChanged();

            // Small delay to ensure screen readers pick up the change
            Task.Delay(100).ContinueWith(_ =>
            {
                _instance._message = message;
                _instance.InvokeAsync(_instance.StateHasChanged);
            }, TaskScheduler.Default);
        }
    }

    public void Dispose()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
#pragma warning restore S2696
