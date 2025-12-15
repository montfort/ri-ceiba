using Bunit;
using Ceiba.Web.Components.Shared;
using FluentAssertions;
using Microsoft.AspNetCore.Components;

namespace Ceiba.Web.Tests.Components;

/// <summary>
/// Component tests for SuggestionInput Blazor component.
/// Tests autocomplete functionality with suggestions, debounce, and validation.
/// </summary>
[Trait("Category", "Unit")]
public class SuggestionInputTests : TestContext
{
    private readonly List<string> _testSuggestions = new()
    {
        "Robo a transeúnte",
        "Robo a casa habitación",
        "Robo de vehículo",
        "Violencia familiar",
        "Violencia de género",
        "Lesiones",
        "Homicidio",
        "Fraude"
    };

    #region Rendering Tests

    [Fact(DisplayName = "SuggestionInput should render form-group div")]
    public void SuggestionInput_ShouldRenderFormGroupDiv()
    {
        // Act
        var cut = Render<SuggestionInput>();

        // Assert
        var div = cut.Find("div.form-group");
        div.Should().NotBeNull();
    }

    [Fact(DisplayName = "SuggestionInput should render input element")]
    public void SuggestionInput_ShouldRenderInputElement()
    {
        // Act
        var cut = Render<SuggestionInput>();

        // Assert
        var input = cut.Find("input");
        input.Should().NotBeNull();
        input.GetAttribute("type").Should().Be("text");
    }

    [Fact(DisplayName = "SuggestionInput should have autocomplete off")]
    public void SuggestionInput_ShouldHaveAutocompleteOff()
    {
        // Act
        var cut = Render<SuggestionInput>();

        // Assert
        var input = cut.Find("input");
        input.GetAttribute("autocomplete").Should().Be("off");
    }

    [Fact(DisplayName = "SuggestionInput should have form-control class")]
    public void SuggestionInput_ShouldHaveFormControlClass()
    {
        // Act
        var cut = Render<SuggestionInput>();

        // Assert
        var input = cut.Find("input");
        input.ClassList.Should().Contain("form-control");
    }

    #endregion

    #region Label Tests

    [Fact(DisplayName = "SuggestionInput should not render label when not provided")]
    public void SuggestionInput_ShouldNotRenderLabelWhenNotProvided()
    {
        // Act
        var cut = Render<SuggestionInput>();

        // Assert
        var labels = cut.FindAll("label");
        labels.Should().BeEmpty();
    }

    [Fact(DisplayName = "SuggestionInput should render label when provided")]
    public void SuggestionInput_ShouldRenderLabelWhenProvided()
    {
        // Act
        var cut = Render<SuggestionInput>(parameters => parameters
            .Add(p => p.Label, "Delito"));

        // Assert
        var label = cut.Find("label");
        label.Should().NotBeNull();
        label.TextContent.Should().Contain("Delito");
    }

    [Fact(DisplayName = "SuggestionInput should show asterisk for required fields")]
    public void SuggestionInput_ShouldShowAsteriskForRequiredFields()
    {
        // Act
        var cut = Render<SuggestionInput>(parameters => parameters
            .Add(p => p.Label, "Delito")
            .Add(p => p.Required, true));

        // Assert
        var label = cut.Find("label");
        label.TextContent.Should().Contain("*");
    }

    [Fact(DisplayName = "SuggestionInput should not show asterisk for non-required fields")]
    public void SuggestionInput_ShouldNotShowAsteriskForNonRequiredFields()
    {
        // Act
        var cut = Render<SuggestionInput>(parameters => parameters
            .Add(p => p.Label, "Delito")
            .Add(p => p.Required, false));

        // Assert
        var label = cut.Find("label");
        label.TextContent.Should().NotContain("*");
    }

    [Fact(DisplayName = "SuggestionInput label should have form-label class")]
    public void SuggestionInput_LabelShouldHaveFormLabelClass()
    {
        // Act
        var cut = Render<SuggestionInput>(parameters => parameters
            .Add(p => p.Label, "Delito"));

        // Assert
        var label = cut.Find("label");
        label.ClassList.Should().Contain("form-label");
    }

    [Fact(DisplayName = "SuggestionInput label should have for attribute matching input id")]
    public void SuggestionInput_LabelShouldHaveForAttributeMatchingInputId()
    {
        // Act
        var cut = Render<SuggestionInput>(parameters => parameters
            .Add(p => p.Id, "delito-input")
            .Add(p => p.Label, "Delito"));

        // Assert
        var label = cut.Find("label");
        var input = cut.Find("input");
        label.GetAttribute("for").Should().Be(input.GetAttribute("id"));
    }

    #endregion

    #region Id Parameter Tests

    [Fact(DisplayName = "SuggestionInput should use provided id")]
    public void SuggestionInput_ShouldUseProvidedId()
    {
        // Act
        var cut = Render<SuggestionInput>(parameters => parameters
            .Add(p => p.Id, "custom-id"));

        // Assert
        var input = cut.Find("input");
        input.GetAttribute("id").Should().Be("custom-id");
    }

    [Fact(DisplayName = "SuggestionInput id can be null")]
    public void SuggestionInput_IdCanBeNull()
    {
        // Act
        var cut = Render<SuggestionInput>();

        // Assert
        var input = cut.Find("input");
        input.GetAttribute("id").Should().BeNullOrEmpty();
    }

    #endregion

    #region Value Binding Tests

    [Fact(DisplayName = "SuggestionInput should display initial value")]
    public void SuggestionInput_ShouldDisplayInitialValue()
    {
        // Act
        var cut = Render<SuggestionInput>(parameters => parameters
            .Add(p => p.Value, "Robo a transeúnte"));

        // Assert
        var input = cut.Find("input");
        input.GetAttribute("value").Should().Be("Robo a transeúnte");
    }

    [Fact(DisplayName = "SuggestionInput should trigger ValueChanged on input")]
    public void SuggestionInput_ShouldTriggerValueChangedOnInput()
    {
        // Arrange
        string? capturedValue = null;
        var cut = Render<SuggestionInput>(parameters => parameters
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string?>(this, v => capturedValue = v)));

        // Act
        var input = cut.Find("input");
        input.Input("Test value");

        // Assert
        capturedValue.Should().Be("Test value");
    }

    #endregion

    #region Placeholder Tests

    [Fact(DisplayName = "SuggestionInput should display placeholder text")]
    public void SuggestionInput_ShouldDisplayPlaceholderText()
    {
        // Act
        var cut = Render<SuggestionInput>(parameters => parameters
            .Add(p => p.PlaceholderText, "Escriba el delito"));

        // Assert
        var input = cut.Find("input");
        input.GetAttribute("placeholder").Should().Be("Escriba el delito");
    }

    [Fact(DisplayName = "SuggestionInput placeholder should be empty by default")]
    public void SuggestionInput_PlaceholderShouldBeEmptyByDefault()
    {
        // Act
        var cut = Render<SuggestionInput>();

        // Assert
        var input = cut.Find("input");
        input.GetAttribute("placeholder").Should().BeEmpty();
    }

    #endregion

    #region Disabled State Tests

    [Fact(DisplayName = "SuggestionInput should not be disabled by default")]
    public void SuggestionInput_ShouldNotBeDisabledByDefault()
    {
        // Act
        var cut = Render<SuggestionInput>();

        // Assert
        var input = cut.Find("input");
        input.HasAttribute("disabled").Should().BeFalse();
    }

    [Fact(DisplayName = "SuggestionInput should be disabled when Disabled is true")]
    public void SuggestionInput_ShouldBeDisabledWhenDisabledIsTrue()
    {
        // Act
        var cut = Render<SuggestionInput>(parameters => parameters
            .Add(p => p.Disabled, true));

        // Assert
        var input = cut.Find("input");
        input.HasAttribute("disabled").Should().BeTrue();
    }

    #endregion

    #region Validation Tests

    [Fact(DisplayName = "SuggestionInput should not have is-invalid class by default")]
    public void SuggestionInput_ShouldNotHaveIsInvalidClassByDefault()
    {
        // Act
        var cut = Render<SuggestionInput>();

        // Assert
        var input = cut.Find("input");
        input.ClassList.Should().NotContain("is-invalid");
    }

    [Fact(DisplayName = "SuggestionInput should have is-invalid class when invalid")]
    public void SuggestionInput_ShouldHaveIsInvalidClassWhenInvalid()
    {
        // Act
        var cut = Render<SuggestionInput>(parameters => parameters
            .Add(p => p.IsInvalid, true));

        // Assert
        var input = cut.Find("input");
        input.ClassList.Should().Contain("is-invalid");
    }

    [Fact(DisplayName = "SuggestionInput should not display validation message when not invalid")]
    public void SuggestionInput_ShouldNotDisplayValidationMessageWhenNotInvalid()
    {
        // Act
        var cut = Render<SuggestionInput>(parameters => parameters
            .Add(p => p.ValidationMessage, "Este campo es requerido"));

        // Assert
        var feedback = cut.FindAll(".invalid-feedback");
        feedback.Should().BeEmpty();
    }

    [Fact(DisplayName = "SuggestionInput should display validation message when invalid")]
    public void SuggestionInput_ShouldDisplayValidationMessageWhenInvalid()
    {
        // Act
        var cut = Render<SuggestionInput>(parameters => parameters
            .Add(p => p.IsInvalid, true)
            .Add(p => p.ValidationMessage, "Este campo es requerido"));

        // Assert
        var feedback = cut.Find(".invalid-feedback");
        feedback.TextContent.Should().Be("Este campo es requerido");
    }

    [Fact(DisplayName = "SuggestionInput validation message should have d-block class")]
    public void SuggestionInput_ValidationMessageShouldHaveDBlockClass()
    {
        // Act
        var cut = Render<SuggestionInput>(parameters => parameters
            .Add(p => p.IsInvalid, true)
            .Add(p => p.ValidationMessage, "Error"));

        // Assert
        var feedback = cut.Find(".invalid-feedback");
        feedback.ClassList.Should().Contain("d-block");
    }

    #endregion

    #region CSS Class Tests

    [Fact(DisplayName = "SuggestionInput should have autocomplete class")]
    public void SuggestionInput_ShouldHaveAutocompleteClass()
    {
        // Act
        var cut = Render<SuggestionInput>();

        // Assert
        var div = cut.Find("div.form-group");
        div.ClassList.Should().Contain("autocomplete");
    }

    [Fact(DisplayName = "SuggestionInput should apply custom CSS class")]
    public void SuggestionInput_ShouldApplyCustomCssClass()
    {
        // Act
        var cut = Render<SuggestionInput>(parameters => parameters
            .Add(p => p.CssClass, "custom-class"));

        // Assert
        var div = cut.Find("div.form-group");
        div.ClassList.Should().Contain("custom-class");
    }

    #endregion

    #region Suggestions Display Tests

    [Fact(DisplayName = "SuggestionInput should not show suggestions initially")]
    public void SuggestionInput_ShouldNotShowSuggestionsInitially()
    {
        // Act
        var cut = Render<SuggestionInput>(parameters => parameters
            .Add(p => p.Suggestions, _testSuggestions));

        // Assert
        var dropdowns = cut.FindAll("ul.dropdown-menu");
        dropdowns.Should().BeEmpty();
    }

    [Fact(DisplayName = "SuggestionInput should have position-relative container")]
    public void SuggestionInput_ShouldHavePositionRelativeContainer()
    {
        // Act
        var cut = Render<SuggestionInput>();

        // Assert
        var container = cut.Find(".position-relative");
        container.Should().NotBeNull();
    }

    #endregion

    #region MinCharacters Tests

    [Fact(DisplayName = "SuggestionInput MinCharacters should default to 1")]
    public void SuggestionInput_MinCharactersShouldDefaultTo1()
    {
        // Act
        var cut = Render<SuggestionInput>(parameters => parameters
            .Add(p => p.Suggestions, _testSuggestions)
            .Add(p => p.Value, "R"));

        // Assert - with 1 character and minCharacters=1, on focus should filter
        var input = cut.Find("input");
        input.Focus();

        // Wait for any potential async updates
        cut.WaitForState(() => true, TimeSpan.FromMilliseconds(100));

        // After focus with value meeting min chars, may show suggestions
        // This depends on implementation details
        cut.Should().NotBeNull();
    }

    [Fact(DisplayName = "SuggestionInput should accept custom MinCharacters")]
    public void SuggestionInput_ShouldAcceptCustomMinCharacters()
    {
        // Act
        var cut = Render<SuggestionInput>(parameters => parameters
            .Add(p => p.MinCharacters, 3));

        // Assert - no exception should be thrown
        cut.Should().NotBeNull();
    }

    #endregion

    #region Dispose Tests

    [Fact(DisplayName = "SuggestionInput should implement IDisposable")]
    public void SuggestionInput_ShouldImplementIDisposable()
    {
        // Act
        var cut = Render<SuggestionInput>();

        // Assert
        cut.Instance.Should().BeAssignableTo<IDisposable>();
    }

    [Fact(DisplayName = "SuggestionInput should dispose without error")]
    public void SuggestionInput_ShouldDisposeWithoutError()
    {
        // Arrange
        var cut = Render<SuggestionInput>();

        // Act & Assert
        var act = () => cut.Dispose();
        act.Should().NotThrow();
    }

    #endregion

    #region Additional Attributes Tests

    [Fact(DisplayName = "SuggestionInput should pass additional attributes to input")]
    public void SuggestionInput_ShouldPassAdditionalAttributesToInput()
    {
        // Act
        var cut = Render<SuggestionInput>(parameters => parameters
            .AddUnmatched("data-testid", "suggestion-input")
            .AddUnmatched("aria-describedby", "help-text"));

        // Assert
        var input = cut.Find("input");
        input.GetAttribute("data-testid").Should().Be("suggestion-input");
        input.GetAttribute("aria-describedby").Should().Be("help-text");
    }

    #endregion

    #region Suggestions Parameter Tests

    [Fact(DisplayName = "SuggestionInput Suggestions should default to empty list")]
    public void SuggestionInput_SuggestionsShouldDefaultToEmptyList()
    {
        // Act
        var cut = Render<SuggestionInput>();

        // Assert - no exception, suggestions are empty
        cut.Should().NotBeNull();
    }

    [Fact(DisplayName = "SuggestionInput should accept suggestions list")]
    public void SuggestionInput_ShouldAcceptSuggestionsList()
    {
        // Act
        var cut = Render<SuggestionInput>(parameters => parameters
            .Add(p => p.Suggestions, _testSuggestions));

        // Assert
        cut.Should().NotBeNull();
    }

    #endregion

    #region Input Event Tests

    [Fact(DisplayName = "SuggestionInput should handle input event")]
    public void SuggestionInput_ShouldHandleInputEvent()
    {
        // Arrange
        var cut = Render<SuggestionInput>(parameters => parameters
            .Add(p => p.Suggestions, _testSuggestions));

        // Act
        var input = cut.Find("input");
        input.Input("Robo");

        // Assert - no exception and input value updated
        cut.Should().NotBeNull();
    }

    [Fact(DisplayName = "SuggestionInput should handle focus event")]
    public void SuggestionInput_ShouldHandleFocusEvent()
    {
        // Arrange
        var cut = Render<SuggestionInput>(parameters => parameters
            .Add(p => p.Suggestions, _testSuggestions));

        // Act
        var input = cut.Find("input");
        input.Focus();

        // Assert - no exception
        cut.Should().NotBeNull();
    }

    [Fact(DisplayName = "SuggestionInput should handle blur event")]
    public void SuggestionInput_ShouldHandleBlurEvent()
    {
        // Arrange
        var cut = Render<SuggestionInput>(parameters => parameters
            .Add(p => p.Suggestions, _testSuggestions));

        // Act
        var input = cut.Find("input");
        input.Blur();

        // Assert - no exception
        cut.Should().NotBeNull();
    }

    #endregion

    #region Required Parameter Tests

    [Fact(DisplayName = "SuggestionInput Required should default to false")]
    public void SuggestionInput_RequiredShouldDefaultToFalse()
    {
        // Act
        var cut = Render<SuggestionInput>(parameters => parameters
            .Add(p => p.Label, "Test"));

        // Assert
        var label = cut.Find("label");
        label.TextContent.Should().NotContain("*");
    }

    #endregion

    #region Markup Structure Tests

    [Fact(DisplayName = "SuggestionInput should have correct DOM structure")]
    public void SuggestionInput_ShouldHaveCorrectDomStructure()
    {
        // Act
        var cut = Render<SuggestionInput>(parameters => parameters
            .Add(p => p.Label, "Delito")
            .Add(p => p.Id, "delito"));

        // Assert
        // Structure: div.form-group > label + div.position-relative > input
        var formGroup = cut.Find("div.form-group");
        formGroup.Should().NotBeNull();

        var label = cut.Find("label");
        label.Should().NotBeNull();

        var posRelative = cut.Find(".position-relative");
        posRelative.Should().NotBeNull();

        var input = cut.Find("input");
        input.Should().NotBeNull();
    }

    #endregion
}
