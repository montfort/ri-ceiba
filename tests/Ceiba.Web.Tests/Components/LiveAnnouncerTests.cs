using Bunit;
using Ceiba.Web.Components.Shared;
using FluentAssertions;

namespace Ceiba.Web.Tests.Components;

/// <summary>
/// Component tests for LiveAnnouncer Blazor component.
/// Tests WCAG 4.1.3 accessibility live region for screen reader announcements.
/// </summary>
[Trait("Category", "Unit")]
public class LiveAnnouncerTests : TestContext
{
    #region Rendering Tests

    [Fact(DisplayName = "LiveAnnouncer should render div element")]
    public void LiveAnnouncer_ShouldRenderDivElement()
    {
        // Act
        var cut = Render<LiveAnnouncer>();

        // Assert
        var div = cut.Find("div");
        div.Should().NotBeNull();
    }

    [Fact(DisplayName = "LiveAnnouncer should have live-announcer id")]
    public void LiveAnnouncer_ShouldHaveLiveAnnouncerId()
    {
        // Act
        var cut = Render<LiveAnnouncer>();

        // Assert
        var div = cut.Find("#live-announcer");
        div.Should().NotBeNull();
    }

    [Fact(DisplayName = "LiveAnnouncer should have visually-hidden CSS class")]
    public void LiveAnnouncer_ShouldHaveVisuallyHiddenCssClass()
    {
        // Act
        var cut = Render<LiveAnnouncer>();

        // Assert
        var div = cut.Find("div");
        div.ClassList.Should().Contain("visually-hidden");
    }

    [Fact(DisplayName = "LiveAnnouncer should have role status")]
    public void LiveAnnouncer_ShouldHaveRoleStatus()
    {
        // Act
        var cut = Render<LiveAnnouncer>();

        // Assert
        var div = cut.Find("div");
        div.GetAttribute("role").Should().Be("status");
    }

    [Fact(DisplayName = "LiveAnnouncer should have aria-atomic true")]
    public void LiveAnnouncer_ShouldHaveAriaAtomicTrue()
    {
        // Act
        var cut = Render<LiveAnnouncer>();

        // Assert
        var div = cut.Find("div");
        div.GetAttribute("aria-atomic").Should().Be("true");
    }

    #endregion

    #region Politeness Parameter Tests

    [Fact(DisplayName = "LiveAnnouncer should default to polite aria-live")]
    public void LiveAnnouncer_ShouldDefaultToPoliteAriaLive()
    {
        // Act
        var cut = Render<LiveAnnouncer>();

        // Assert
        var div = cut.Find("div");
        div.GetAttribute("aria-live").Should().Be("polite");
    }

    [Fact(DisplayName = "LiveAnnouncer should accept assertive politeness")]
    public void LiveAnnouncer_ShouldAcceptAssertivePoliteness()
    {
        // Act
        var cut = Render<LiveAnnouncer>(parameters => parameters
            .Add(p => p.Politeness, "assertive"));

        // Assert
        var div = cut.Find("div");
        div.GetAttribute("aria-live").Should().Be("assertive");
    }

    [Fact(DisplayName = "LiveAnnouncer should accept off politeness")]
    public void LiveAnnouncer_ShouldAcceptOffPoliteness()
    {
        // Act
        var cut = Render<LiveAnnouncer>(parameters => parameters
            .Add(p => p.Politeness, "off"));

        // Assert
        var div = cut.Find("div");
        div.GetAttribute("aria-live").Should().Be("off");
    }

    [Fact(DisplayName = "LiveAnnouncer should accept custom politeness value")]
    public void LiveAnnouncer_ShouldAcceptCustomPolitenessValue()
    {
        // Act
        var cut = Render<LiveAnnouncer>(parameters => parameters
            .Add(p => p.Politeness, "custom-value"));

        // Assert
        var div = cut.Find("div");
        div.GetAttribute("aria-live").Should().Be("custom-value");
    }

    #endregion

    #region Accessibility Tests

    [Fact(DisplayName = "LiveAnnouncer should implement WCAG 4.1.3 live region")]
    public void LiveAnnouncer_ShouldImplementWcagLiveRegion()
    {
        // Act
        var cut = Render<LiveAnnouncer>();

        // Assert - WCAG 4.1.3 requires aria-live for status messages
        var div = cut.Find("div");
        div.GetAttribute("aria-live").Should().NotBeNullOrEmpty();
        div.GetAttribute("role").Should().Be("status");
    }

    [Fact(DisplayName = "LiveAnnouncer should be hidden from visual display")]
    public void LiveAnnouncer_ShouldBeHiddenFromVisualDisplay()
    {
        // Act
        var cut = Render<LiveAnnouncer>();

        // Assert - visually hidden but accessible to screen readers
        var div = cut.Find("div");
        div.ClassList.Should().Contain("visually-hidden");
    }

    [Fact(DisplayName = "LiveAnnouncer should announce entire content at once")]
    public void LiveAnnouncer_ShouldAnnounceEntireContentAtOnce()
    {
        // Act
        var cut = Render<LiveAnnouncer>();

        // Assert - aria-atomic="true" ensures the whole region is announced
        var div = cut.Find("div");
        div.GetAttribute("aria-atomic").Should().Be("true");
    }

    #endregion

    #region Markup Structure Tests

    [Fact(DisplayName = "LiveAnnouncer should render minimal markup")]
    public void LiveAnnouncer_ShouldRenderMinimalMarkup()
    {
        // Act
        var cut = Render<LiveAnnouncer>();

        // Assert - should be just a div element
        var elements = cut.FindAll("*");
        elements.Should().HaveCount(1); // Just the div
    }

    [Fact(DisplayName = "LiveAnnouncer should have all required ARIA attributes")]
    public void LiveAnnouncer_ShouldHaveAllRequiredAriaAttributes()
    {
        // Act
        var cut = Render<LiveAnnouncer>();

        // Assert
        var div = cut.Find("div");
        div.GetAttribute("aria-live").Should().NotBeNull();
        div.GetAttribute("aria-atomic").Should().NotBeNull();
        div.GetAttribute("role").Should().NotBeNull();
        div.GetAttribute("id").Should().NotBeNull();
        div.GetAttribute("class").Should().NotBeNull();
    }

    [Fact(DisplayName = "LiveAnnouncer initial content should be empty")]
    public void LiveAnnouncer_InitialContentShouldBeEmpty()
    {
        // Act
        var cut = Render<LiveAnnouncer>();

        // Assert
        var div = cut.Find("div");
        div.TextContent.Trim().Should().BeEmpty();
    }

    #endregion

    #region Dispose Tests

    [Fact(DisplayName = "LiveAnnouncer should implement IDisposable")]
    public void LiveAnnouncer_ShouldImplementIDisposable()
    {
        // Act
        var cut = Render<LiveAnnouncer>();

        // Assert
        cut.Instance.Should().BeAssignableTo<IDisposable>();
    }

    [Fact(DisplayName = "LiveAnnouncer should dispose without error")]
    public void LiveAnnouncer_ShouldDisposeWithoutError()
    {
        // Arrange
        var cut = Render<LiveAnnouncer>();

        // Act & Assert
        var act = () => cut.Dispose();
        act.Should().NotThrow();
    }

    #endregion

    #region Multiple Instance Tests

    [Fact(DisplayName = "LiveAnnouncer can render multiple instances")]
    public void LiveAnnouncer_CanRenderMultipleInstances()
    {
        // Act
        var cut1 = Render<LiveAnnouncer>();
        var cut2 = Render<LiveAnnouncer>();

        // Assert
        cut1.Should().NotBeNull();
        cut2.Should().NotBeNull();
        cut1.Find("div").Should().NotBeNull();
        cut2.Find("div").Should().NotBeNull();
    }

    [Fact(DisplayName = "Each LiveAnnouncer instance should have same id")]
    public void LiveAnnouncer_EachInstanceShouldHaveSameId()
    {
        // Act
        var cut1 = Render<LiveAnnouncer>();
        var cut2 = Render<LiveAnnouncer>();

        // Assert - using same id is intentional for singleton pattern
        cut1.Find("div").GetAttribute("id").Should().Be("live-announcer");
        cut2.Find("div").GetAttribute("id").Should().Be("live-announcer");
    }

    #endregion
}
