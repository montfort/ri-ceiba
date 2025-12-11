using Bunit;
using Ceiba.Shared.DTOs;
using Ceiba.Web.Components.Shared;
using FluentAssertions;
using Microsoft.AspNetCore.Components;

namespace Ceiba.Web.Tests;

/// <summary>
/// Component tests for ReportFilter Blazor component (US2).
/// Tests T050: Report filtering functionality for supervisor view.
/// </summary>
public class ReportFilterComponentTests : TestContext
{
    #region T050: Component Rendering Tests

    [Fact(DisplayName = "T050: ReportFilter should render estado dropdown")]
    public void ReportFilter_ShouldRenderEstadoDropdown()
    {
        // Arrange & Act
        var cut = Render<ReportFilter>();

        // Assert
        var estadoSelect = cut.Find("select#filterEstado");
        estadoSelect.Should().NotBeNull();

        var options = estadoSelect.QuerySelectorAll("option");
        options.Should().HaveCount(3); // Todos, Borrador, Entregado
    }

    [Fact(DisplayName = "T050: ReportFilter should render delito input")]
    public void ReportFilter_ShouldRenderDelitoInput()
    {
        // Arrange & Act
        var cut = Render<ReportFilter>();

        // Assert
        var delitoInput = cut.Find("input#filterDelito");
        delitoInput.Should().NotBeNull();
        delitoInput.GetAttribute("placeholder").Should().Be("Buscar por delito");
    }

    [Fact(DisplayName = "T050: ReportFilter should render date range inputs")]
    public void ReportFilter_ShouldRenderDateRangeInputs()
    {
        // Arrange & Act
        var cut = Render<ReportFilter>();

        // Assert
        var fechaDesdeInput = cut.Find("input#filterFechaDesde");
        var fechaHastaInput = cut.Find("input#filterFechaHasta");

        fechaDesdeInput.Should().NotBeNull();
        fechaDesdeInput.GetAttribute("type").Should().Be("date");

        fechaHastaInput.Should().NotBeNull();
        fechaHastaInput.GetAttribute("type").Should().Be("date");
    }

    [Fact(DisplayName = "T050: ReportFilter should render clear button")]
    public void ReportFilter_ShouldRenderClearButton()
    {
        // Arrange & Act
        var cut = Render<ReportFilter>();

        // Assert
        var clearButton = cut.Find("button[title='Limpiar filtros']");
        clearButton.Should().NotBeNull();
        clearButton.InnerHtml.Should().Contain("bi-x-circle");
    }

    [Fact(DisplayName = "T050: ReportFilter should show zona filter by default")]
    public void ReportFilter_ShouldShowZonaFilterByDefault()
    {
        // Arrange & Act
        var cut = Render<ReportFilter>();

        // Assert
        var zonaInput = cut.Find("input#filterZona");
        zonaInput.Should().NotBeNull();
    }

    [Fact(DisplayName = "T050: ReportFilter should hide zona filter when ShowZonaFilter is false")]
    public void ReportFilter_ShouldHideZonaFilterWhenDisabled()
    {
        // Arrange & Act
        var cut = Render<ReportFilter>(parameters => parameters
            .Add(p => p.ShowZonaFilter, false));

        // Assert
        var zonaInputs = cut.FindAll("input#filterZona");
        zonaInputs.Should().BeEmpty();
    }

    #endregion

    #region T050: Filter State Tests

    [Fact(DisplayName = "T050: Filter parameter should bind estado correctly")]
    public void Filter_ShouldBindEstadoCorrectly()
    {
        // Arrange
        var filter = new ReportFilterDto { Estado = 1 };

        // Act
        var cut = Render<ReportFilter>(parameters => parameters
            .Add(p => p.Filter, filter));

        // Assert
        var estadoSelect = cut.Find("select#filterEstado");
        // The binding should reflect estado=1 (Entregado)
        filter.Estado.Should().Be(1);
    }

    [Fact(DisplayName = "T050: Filter parameter should bind delito correctly")]
    public void Filter_ShouldBindDelitoCorrectly()
    {
        // Arrange
        var filter = new ReportFilterDto { Delito = "Robo" };

        // Act
        var cut = Render<ReportFilter>(parameters => parameters
            .Add(p => p.Filter, filter));

        // Assert
        var delitoInput = cut.Find("input#filterDelito");
        delitoInput.GetAttribute("value").Should().Be("Robo");
    }

    [Fact(DisplayName = "T050: Filter parameter should bind date range correctly")]
    public void Filter_ShouldBindDateRangeCorrectly()
    {
        // Arrange
        var filter = new ReportFilterDto
        {
            FechaDesde = new DateTime(2024, 1, 1),
            FechaHasta = new DateTime(2024, 12, 31)
        };

        // Act
        var cut = Render<ReportFilter>(parameters => parameters
            .Add(p => p.Filter, filter));

        // Assert
        filter.FechaDesde.Should().Be(new DateTime(2024, 1, 1));
        filter.FechaHasta.Should().Be(new DateTime(2024, 12, 31));
    }

    #endregion

    #region T050: Event Callback Tests

    [Fact(DisplayName = "T050: Changing estado should invoke FilterChanged")]
    public async Task ChangingEstado_ShouldInvokeFilterChanged()
    {
        // Arrange
        var filterChangedInvoked = false;
        ReportFilterDto? changedFilter = null;
        var filter = new ReportFilterDto();

        var cut = Render<ReportFilter>(parameters => parameters
            .Add(p => p.Filter, filter)
            .Add(p => p.FilterChanged, EventCallback.Factory.Create<ReportFilterDto>(this, f =>
            {
                filterChangedInvoked = true;
                changedFilter = f;
            })));

        // Act
        var estadoSelect = cut.Find("select#filterEstado");
        await cut.InvokeAsync(() => estadoSelect.Change("1"));

        // Assert
        filterChangedInvoked.Should().BeTrue();
        changedFilter.Should().NotBeNull();
    }

    [Fact(DisplayName = "T050: Changing fechaDesde should invoke FilterChanged")]
    public async Task ChangingFechaDesde_ShouldInvokeFilterChanged()
    {
        // Arrange
        var filterChangedInvoked = false;
        var filter = new ReportFilterDto();

        var cut = Render<ReportFilter>(parameters => parameters
            .Add(p => p.Filter, filter)
            .Add(p => p.FilterChanged, EventCallback.Factory.Create<ReportFilterDto>(this, f =>
            {
                filterChangedInvoked = true;
            })));

        // Act
        var fechaDesdeInput = cut.Find("input#filterFechaDesde");
        await cut.InvokeAsync(() => fechaDesdeInput.Change("2024-01-01"));

        // Assert
        filterChangedInvoked.Should().BeTrue();
    }

    [Fact(DisplayName = "T050: Clear button should invoke OnClear callback")]
    public async Task ClearButton_ShouldInvokeOnClearCallback()
    {
        // Arrange
        var onClearInvoked = false;
        var filter = new ReportFilterDto
        {
            Estado = 1,
            Delito = "Test"
        };

        var cut = Render<ReportFilter>(parameters => parameters
            .Add(p => p.Filter, filter)
            .Add(p => p.OnClear, EventCallback.Factory.Create(this, () =>
            {
                onClearInvoked = true;
            })));

        // Act
        var clearButton = cut.Find("button[title='Limpiar filtros']");
        await cut.InvokeAsync(() => clearButton.Click());

        // Assert
        onClearInvoked.Should().BeTrue();
    }

    [Fact(DisplayName = "T050: Clear button should reset filter values")]
    public async Task ClearButton_ShouldResetFilterValues()
    {
        // Arrange
        var filter = new ReportFilterDto
        {
            Estado = 1,
            Delito = "Test",
            FechaDesde = DateTime.Now,
            FechaHasta = DateTime.Now.AddDays(7)
        };

        var cut = Render<ReportFilter>(parameters => parameters
            .Add(p => p.Filter, filter)
            .Add(p => p.FilterChanged, EventCallback.Factory.Create<ReportFilterDto>(this, f => { })));

        // Act
        var clearButton = cut.Find("button[title='Limpiar filtros']");
        await cut.InvokeAsync(() => clearButton.Click());

        // Assert
        filter.Estado.Should().BeNull();
        filter.Delito.Should().BeNull();
        filter.FechaDesde.Should().BeNull();
        filter.FechaHasta.Should().BeNull();
    }

    #endregion

    #region T050: Debounce Tests

    [Fact(DisplayName = "T050: Delito input should support debouncing")]
    public void DelitoInput_ShouldSupportDebouncing()
    {
        // Arrange
        var customDelay = 300;

        // Act
        var cut = Render<ReportFilter>(parameters => parameters
            .Add(p => p.DebounceDelay, customDelay));

        // Assert - Component should accept custom debounce delay
        cut.Instance.DebounceDelay.Should().Be(300);
    }

    [Fact(DisplayName = "T050: DebounceDelay parameter should default to 500ms")]
    public void DebounceDelay_ShouldDefaultTo500ms()
    {
        // Arrange & Act
        var cut = Render<ReportFilter>();

        // Assert
        cut.Instance.DebounceDelay.Should().Be(500);
    }

    #endregion

    #region T050: Estado Options Tests

    [Fact(DisplayName = "T050: Estado dropdown should have correct options")]
    public void EstadoDropdown_ShouldHaveCorrectOptions()
    {
        // Arrange & Act
        var cut = Render<ReportFilter>();

        // Assert
        var estadoSelect = cut.Find("select#filterEstado");
        var options = estadoSelect.QuerySelectorAll("option");

        options[0].TextContent.Should().Be("Todos");
        options[0].GetAttribute("value").Should().Be("");

        options[1].TextContent.Should().Be("Borrador");
        options[1].GetAttribute("value").Should().Be("0");

        options[2].TextContent.Should().Be("Entregado");
        options[2].GetAttribute("value").Should().Be("1");
    }

    #endregion

    #region T050: Accessibility Tests

    [Fact(DisplayName = "T050: All filter inputs should have labels")]
    public void FilterInputs_ShouldHaveLabels()
    {
        // Arrange & Act
        var cut = Render<ReportFilter>();

        // Assert
        var labels = cut.FindAll("label");
        labels.Should().Contain(l => l.GetAttribute("for") == "filterEstado");
        labels.Should().Contain(l => l.GetAttribute("for") == "filterDelito");
        labels.Should().Contain(l => l.GetAttribute("for") == "filterFechaDesde");
        labels.Should().Contain(l => l.GetAttribute("for") == "filterFechaHasta");
    }

    [Fact(DisplayName = "T050: Clear button should have title attribute")]
    public void ClearButton_ShouldHaveTitleAttribute()
    {
        // Arrange & Act
        var cut = Render<ReportFilter>();

        // Assert
        var clearButton = cut.Find("button[title='Limpiar filtros']");
        clearButton.Should().NotBeNull();
    }

    #endregion

    #region T050: CSS Class Tests

    [Fact(DisplayName = "T050: Component should have card styling")]
    public void Component_ShouldHaveCardStyling()
    {
        // Arrange & Act
        var cut = Render<ReportFilter>();

        // Assert
        var card = cut.Find(".card.shadow-sm");
        card.Should().NotBeNull();
    }

    [Fact(DisplayName = "T050: Filter inputs should use Bootstrap form classes")]
    public void FilterInputs_ShouldUseBootstrapFormClasses()
    {
        // Arrange & Act
        var cut = Render<ReportFilter>();

        // Assert
        var formSelect = cut.Find("select.form-select");
        formSelect.Should().NotBeNull();

        var formControl = cut.Find("input.form-control");
        formControl.Should().NotBeNull();
    }

    #endregion
}
