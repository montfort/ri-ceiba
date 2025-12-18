# Guía: Componentes Blazor

Esta guía cubre patrones y mejores prácticas para crear componentes Blazor en el proyecto.

## Estructura de Componentes

### Página (Page Component)

```razor
@page "/reports/new"
@using Ceiba.Core.Interfaces
@using Ceiba.Shared.DTOs
@inject IReportService ReportService
@inject NavigationManager Navigation
@attribute [Authorize(Roles = "CREADOR")]

<PageTitle>Nuevo Reporte</PageTitle>

<div class="container-fluid py-4">
    <!-- Contenido de la página -->
</div>

@code {
    // Propiedades y parámetros
    [Parameter] public int? ReportId { get; set; }

    private ReportDto? Report { get; set; }

    // Lifecycle
    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    // Métodos privados
    private async Task LoadDataAsync()
    {
        // ...
    }
}
```

### Componente Reutilizable

```razor
@typeparam TValue

<div class="mb-3">
    <label for="@Id" class="form-label">@Label @(Required ? "*" : "")</label>
    <select id="@Id"
            class="form-select @(Disabled ? "disabled" : "")"
            value="@Value"
            @onchange="OnSelectionChanged"
            disabled="@Disabled">
        <option value="0">@PlaceholderText</option>
        @foreach (var item in Items)
        {
            <option value="@item.Id">@item.Nombre</option>
        }
    </select>
</div>

@code {
    [Parameter] public string Id { get; set; } = "";
    [Parameter] public string Label { get; set; } = "";
    [Parameter] public TValue? Value { get; set; }
    [Parameter] public EventCallback<TValue> ValueChanged { get; set; }
    [Parameter] public List<CatalogItemDto> Items { get; set; } = new();
    [Parameter] public bool Required { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public string PlaceholderText { get; set; } = "Seleccione...";

    private async Task OnSelectionChanged(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out var value))
        {
            await ValueChanged.InvokeAsync((TValue)(object)value);
        }
    }
}
```

## Patrones Comunes

### Two-Way Binding Personalizado

```razor
@* Padre *@
<MyInput @bind-Value="Model.Name" />

@* Componente *@
@code {
    [Parameter] public string Value { get; set; }
    [Parameter] public EventCallback<string> ValueChanged { get; set; }

    private async Task OnChange(ChangeEventArgs e)
    {
        await ValueChanged.InvokeAsync(e.Value?.ToString());
    }
}
```

### Cascading Values

```razor
@* Layout o componente padre *@
<CascadingValue Value="@CurrentUser">
    @Body
</CascadingValue>

@* Componente hijo *@
@code {
    [CascadingParameter] public UserDto? CurrentUser { get; set; }
}
```

### Event Callbacks

```razor
@* Padre *@
<ReportCard Report="@report" OnDelete="HandleDelete" OnEdit="HandleEdit" />

@* Componente *@
@code {
    [Parameter] public ReportDto Report { get; set; }
    [Parameter] public EventCallback<int> OnDelete { get; set; }
    [Parameter] public EventCallback<int> OnEdit { get; set; }

    private async Task DeleteClicked()
    {
        await OnDelete.InvokeAsync(Report.Id);
    }
}
```

## Manejo de Estado

### Estado Local

```csharp
@code {
    private bool IsLoading { get; set; } = true;
    private string? ErrorMessage { get; set; }
    private List<ReportDto> Reports { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        try
        {
            IsLoading = true;
            Reports = await ReportService.GetReportsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error al cargar datos";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

### Forzar Re-render

```csharp
StateHasChanged(); // Actualiza el UI
await InvokeAsync(StateHasChanged); // Desde otro thread
```

## Formularios

### Con EditForm

```razor
<EditForm Model="@Model" OnValidSubmit="HandleSubmit">
    <DataAnnotationsValidator />
    <ValidationSummary />

    <div class="mb-3">
        <label for="name" class="form-label">Nombre</label>
        <InputText id="name" class="form-control" @bind-Value="Model.Name" />
        <ValidationMessage For="@(() => Model.Name)" />
    </div>

    <button type="submit" class="btn btn-primary" disabled="@IsSubmitting">
        @if (IsSubmitting)
        {
            <span class="spinner-border spinner-border-sm"></span>
        }
        Guardar
    </button>
</EditForm>

@code {
    private MyDto Model { get; set; } = new();
    private bool IsSubmitting { get; set; }

    private async Task HandleSubmit()
    {
        IsSubmitting = true;
        try
        {
            await Service.SaveAsync(Model);
            Navigation.NavigateTo("/list");
        }
        finally
        {
            IsSubmitting = false;
        }
    }
}
```

## JavaScript Interop

### Llamar JavaScript desde C#

```csharp
@inject IJSRuntime JS

@code {
    private async Task DownloadFile(byte[] data, string fileName)
    {
        var base64 = Convert.ToBase64String(data);
        await JS.InvokeVoidAsync("downloadFile", base64, fileName);
    }
}
```

### Recibir Datos de JavaScript

```csharp
var result = await JS.InvokeAsync<string>("getClipboardText");
```

## Autorización

### Basada en Roles

```razor
<AuthorizeView Roles="ADMIN">
    <Authorized>
        <button>Solo para Admin</button>
    </Authorized>
    <NotAuthorized>
        <p>No tienes permisos</p>
    </NotAuthorized>
</AuthorizeView>
```

### Programática

```csharp
@inject AuthenticationStateProvider AuthProvider

@code {
    private bool IsAdmin { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthProvider.GetAuthenticationStateAsync();
        IsAdmin = authState.User.IsInRole("ADMIN");
    }
}
```

## Rendimiento

### Evitar Re-renders Innecesarios

```csharp
// Solo re-renderiza si los parámetros cambian
protected override bool ShouldRender()
{
    return _hasChanged;
}
```

### Virtualización para Listas Grandes

```razor
<Virtualize Items="@LargeList" Context="item">
    <ItemContent>
        <div>@item.Name</div>
    </ItemContent>
</Virtualize>
```

## Testing con bUnit

```csharp
public class ReportFormTests : TestContext
{
    [Fact]
    public void RendersFormFields()
    {
        // Arrange
        Services.AddScoped(_ => Mock.Of<IReportService>());

        // Act
        var cut = RenderComponent<ReportForm>();

        // Assert
        Assert.NotNull(cut.Find("#delito"));
    }

    [Fact]
    public async Task SubmitCallsService()
    {
        var mockService = new Mock<IReportService>();
        Services.AddScoped(_ => mockService.Object);

        var cut = RenderComponent<ReportForm>();

        await cut.Find("form").SubmitAsync();

        mockService.Verify(s => s.CreateReportAsync(It.IsAny<CreateReportDto>(), It.IsAny<Guid>()), Times.Once);
    }
}
```
