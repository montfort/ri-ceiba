using Bunit;
using Ceiba.Web.Components.Shared;
using FluentAssertions;

namespace Ceiba.Web.Tests.Components;

/// <summary>
/// Component tests for SkipLink Blazor component.
/// Tests WCAG 2.4.1 accessibility skip link for keyboard navigation.
/// </summary>
[Trait("Category", "Unit")]
public class SkipLinkTests : TestContext
{
    #region Rendering Tests

    [Fact(DisplayName = "SkipLink should render anchor element")]
    public void SkipLink_ShouldRenderAnchorElement()
    {
        // Act
        var cut = Render<SkipLink>();

        // Assert
        var anchor = cut.Find("a");
        anchor.Should().NotBeNull();
    }

    [Fact(DisplayName = "SkipLink should have href pointing to main-content")]
    public void SkipLink_ShouldHaveHrefToMainContent()
    {
        // Act
        var cut = Render<SkipLink>();

        // Assert
        var anchor = cut.Find("a");
        anchor.GetAttribute("href").Should().Be("#main-content");
    }

    [Fact(DisplayName = "SkipLink should have skip-link CSS class")]
    public void SkipLink_ShouldHaveSkipLinkCssClass()
    {
        // Act
        var cut = Render<SkipLink>();

        // Assert
        var anchor = cut.Find("a");
        anchor.ClassList.Should().Contain("skip-link");
    }

    [Fact(DisplayName = "SkipLink should have visually-hidden-focusable CSS class")]
    public void SkipLink_ShouldHaveVisuallyHiddenFocusableCssClass()
    {
        // Act
        var cut = Render<SkipLink>();

        // Assert
        var anchor = cut.Find("a");
        anchor.ClassList.Should().Contain("visually-hidden-focusable");
    }

    [Fact(DisplayName = "SkipLink should display Spanish text")]
    public void SkipLink_ShouldDisplaySpanishText()
    {
        // Act
        var cut = Render<SkipLink>();

        // Assert
        cut.Markup.Should().Contain("Saltar al contenido principal");
    }

    #endregion

    #region Accessibility Tests

    [Fact(DisplayName = "SkipLink should be first focusable element pattern")]
    public void SkipLink_ShouldBeAnchorElementForKeyboardNavigation()
    {
        // Act
        var cut = Render<SkipLink>();

        // Assert - verify it's an anchor that can receive focus
        var anchor = cut.Find("a");
        anchor.TagName.Should().Be("A");
        anchor.GetAttribute("href").Should().NotBeNullOrEmpty();
    }

    [Fact(DisplayName = "SkipLink should be visually hidden until focused")]
    public void SkipLink_ShouldBeVisuallyHiddenUntilFocused()
    {
        // Act
        var cut = Render<SkipLink>();

        // Assert - visually-hidden-focusable is a Bootstrap class that hides content
        // except when focused via keyboard
        var anchor = cut.Find("a");
        anchor.ClassList.Should().Contain("visually-hidden-focusable");
    }

    [Fact(DisplayName = "SkipLink markup should match WCAG 2.4.1 requirements")]
    public void SkipLink_MarkupShouldMatchWcagRequirements()
    {
        // Act
        var cut = Render<SkipLink>();

        // Assert - WCAG 2.4.1 requires a mechanism to bypass blocks of content
        var anchor = cut.Find("a");

        // Must be a link
        anchor.TagName.Should().Be("A");

        // Must point to main content
        anchor.GetAttribute("href").Should().StartWith("#");

        // Must have descriptive text
        anchor.TextContent.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Markup Structure Tests

    [Fact(DisplayName = "SkipLink should render minimal markup")]
    public void SkipLink_ShouldRenderMinimalMarkup()
    {
        // Act
        var cut = Render<SkipLink>();

        // Assert - should be just an anchor element with text
        var elements = cut.FindAll("*");
        elements.Should().HaveCount(1); // Just the anchor
    }

    [Fact(DisplayName = "SkipLink anchor should not have nested elements")]
    public void SkipLink_AnchorShouldNotHaveNestedElements()
    {
        // Act
        var cut = Render<SkipLink>();

        // Assert
        var anchor = cut.Find("a");
        anchor.ChildElementCount.Should().Be(0);
    }

    [Fact(DisplayName = "SkipLink should have both required CSS classes")]
    public void SkipLink_ShouldHaveBothRequiredCssClasses()
    {
        // Act
        var cut = Render<SkipLink>();

        // Assert
        var anchor = cut.Find("a");
        anchor.ClassList.Should().Contain("skip-link");
        anchor.ClassList.Should().Contain("visually-hidden-focusable");
        anchor.ClassList.Should().HaveCount(2);
    }

    #endregion
}
