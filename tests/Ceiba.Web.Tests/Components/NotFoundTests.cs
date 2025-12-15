using Bunit;
using Ceiba.Web.Components.Pages;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace Ceiba.Web.Tests.Components;

/// <summary>
/// Component tests for NotFound Blazor page component.
/// Tests 404 error page display.
/// </summary>
[Trait("Category", "Unit")]
public class NotFoundTests : TestContext
{
    public NotFoundTests()
    {
        // Register dependencies that might be needed by layout
        Services.AddSingleton<NavigationManager>(new FakeNavigationManager());
        Services.AddSingleton<AuthenticationStateProvider>(new TestAuthStateProvider());

        // Configure to ignore layout for isolated component testing
        Services.AddSingleton<IComponentActivator, TestComponentActivator>();
    }

    #region Rendering Tests

    [Fact(DisplayName = "NotFound should render h3 heading")]
    public void NotFound_ShouldRenderH3Heading()
    {
        // Act
        var cut = Render<NotFound>();

        // Assert
        var heading = cut.Find("h3");
        heading.Should().NotBeNull();
    }

    [Fact(DisplayName = "NotFound should display Not Found text")]
    public void NotFound_ShouldDisplayNotFoundText()
    {
        // Act
        var cut = Render<NotFound>();

        // Assert
        cut.Markup.Should().Contain("Not Found");
    }

    [Fact(DisplayName = "NotFound should display explanatory message")]
    public void NotFound_ShouldDisplayExplanatoryMessage()
    {
        // Act
        var cut = Render<NotFound>();

        // Assert
        cut.Markup.Should().Contain("the content you are looking for does not exist");
    }

    [Fact(DisplayName = "NotFound should render paragraph element")]
    public void NotFound_ShouldRenderParagraphElement()
    {
        // Act
        var cut = Render<NotFound>();

        // Assert
        var paragraph = cut.Find("p");
        paragraph.Should().NotBeNull();
    }

    #endregion

    #region Content Tests

    [Fact(DisplayName = "NotFound heading should say Not Found")]
    public void NotFound_HeadingShouldSayNotFound()
    {
        // Act
        var cut = Render<NotFound>();

        // Assert
        var heading = cut.Find("h3");
        heading.TextContent.Should().Be("Not Found");
    }

    [Fact(DisplayName = "NotFound message should be user-friendly")]
    public void NotFound_MessageShouldBeUserFriendly()
    {
        // Act
        var cut = Render<NotFound>();

        // Assert
        var paragraph = cut.Find("p");
        paragraph.TextContent.Should().Contain("Sorry");
        paragraph.TextContent.Should().Contain("does not exist");
    }

    #endregion

    #region Markup Structure Tests

    [Fact(DisplayName = "NotFound should have minimal markup structure")]
    public void NotFound_ShouldHaveMinimalMarkupStructure()
    {
        // Act
        var cut = Render<NotFound>();

        // Assert - should contain h3 and p elements
        cut.FindAll("h3").Should().HaveCount(1);
        cut.FindAll("p").Should().HaveCount(1);
    }

    [Fact(DisplayName = "NotFound h3 should come before p")]
    public void NotFound_H3ShouldComeBeforeP()
    {
        // Act
        var cut = Render<NotFound>();

        // Assert
        var markup = cut.Markup;
        var h3Index = markup.IndexOf("<h3>");
        var pIndex = markup.IndexOf("<p>");

        h3Index.Should().BeLessThan(pIndex);
    }

    #endregion

    #region Page Route Tests

    [Fact(DisplayName = "NotFound component should be a routable page")]
    public void NotFound_ShouldBeRoutablePage()
    {
        // Arrange - NotFound has @page "/not-found" directive
        var pageAttribute = typeof(NotFound).GetCustomAttributes(typeof(RouteAttribute), false);

        // Assert
        pageAttribute.Should().NotBeEmpty();
    }

    [Fact(DisplayName = "NotFound should have not-found route")]
    public void NotFound_ShouldHaveNotFoundRoute()
    {
        // Arrange
        var routeAttributes = typeof(NotFound).GetCustomAttributes(typeof(RouteAttribute), false)
            .Cast<RouteAttribute>();

        // Assert
        routeAttributes.Should().Contain(r => r.Template == "/not-found");
    }

    #endregion

    #region Accessibility Tests

    [Fact(DisplayName = "NotFound should have proper heading hierarchy")]
    public void NotFound_ShouldHaveProperHeadingHierarchy()
    {
        // Act
        var cut = Render<NotFound>();

        // Assert - h3 is appropriate for a section within a page
        var h3Elements = cut.FindAll("h3");
        h3Elements.Should().HaveCount(1);

        // No h1 or h2 in the component itself (those come from layout)
        cut.FindAll("h1").Should().BeEmpty();
        cut.FindAll("h2").Should().BeEmpty();
    }

    [Fact(DisplayName = "NotFound content should be readable")]
    public void NotFound_ContentShouldBeReadable()
    {
        // Act
        var cut = Render<NotFound>();

        // Assert - text is plain and clear
        cut.Markup.Should().NotContain("undefined");
        cut.Markup.Should().NotContain("null");
        cut.Markup.Should().NotContain("error:");
    }

    #endregion

    #region Helper Classes

    private class FakeNavigationManager : NavigationManager
    {
        public FakeNavigationManager()
        {
            Initialize("https://localhost:5001/", "https://localhost:5001/not-found");
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            if (!uri.StartsWith("http"))
            {
                uri = new Uri(new Uri(BaseUri), uri).ToString();
            }
            Uri = uri;
        }
    }

    private class TestAuthStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "testuser@ceiba.local"),
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            }, "Test");

            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
        }
    }

#pragma warning disable CA1812 // Classes are instantiated via DI
    private sealed class TestComponentActivator : IComponentActivator
    {
        public IComponent CreateInstance(Type componentType)
        {
            // For layout components, return a simple passthrough
            if (componentType.Name.Contains("Layout") || componentType.Name.Contains("NavMenu") ||
                componentType.Name.Contains("ReconnectModal"))
            {
                return Activator.CreateInstance<EmptyLayout>();
            }

            return (IComponent)Activator.CreateInstance(componentType)!;
        }
    }

    private sealed class EmptyLayout : LayoutComponentBase
    {
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.AddContent(0, Body);
        }
    }
#pragma warning restore CA1812

    #endregion
}
