# Guía de Testing (TDD)

El proyecto Ceiba sigue **Test-Driven Development (TDD)** como metodología obligatoria de desarrollo.

## Filosofía TDD

### El Ciclo Red-Green-Refactor

```
     ┌──────────────┐
     │     RED      │  ← Escribe test que falla
     └──────┬───────┘
            │
            ▼
     ┌──────────────┐
     │    GREEN     │  ← Implementa código mínimo
     └──────┬───────┘
            │
            ▼
     ┌──────────────┐
     │   REFACTOR   │  ← Mejora sin romper tests
     └──────┬───────┘
            │
            └──────────► Repetir
```

### Reglas de TDD

1. **No escribas código de producción sin un test que falle**
2. **Escribe solo el código necesario para pasar el test**
3. **Refactoriza cuando los tests pasen**

## Estructura de Tests

```
tests/
├── Ceiba.Core.Tests/           # Tests unitarios de entidades
├── Ceiba.Application.Tests/    # Tests de servicios
├── Ceiba.Infrastructure.Tests/ # Tests de integración
├── Ceiba.Web.Tests/           # Tests de componentes Blazor
└── Ceiba.Integration.Tests/   # Tests E2E (Playwright)
```

## Tipos de Tests

### Unit Tests (Core Layer)

Prueban entidades y lógica de dominio aislada.

```csharp
public class ReporteIncidenciaTests
{
    [Fact]
    public void CannotSubmit_WhenAlreadySubmitted()
    {
        // Arrange
        var reporte = new ReporteIncidencia { Estado = 1 };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => reporte.Submit());
        Assert.Equal("El reporte ya fue entregado", exception.Message);
    }
}
```

### Service Tests (Application Layer)

Prueban servicios con dependencias mockeadas.

```csharp
public class ReportServiceTests
{
    private readonly Mock<CeibaDbContext> _mockContext;
    private readonly ReportService _service;

    public ReportServiceTests()
    {
        _mockContext = new Mock<CeibaDbContext>();
        _service = new ReportService(_mockContext.Object);
    }

    [Fact]
    public async Task CreateReportAsync_ShouldCreateReport()
    {
        // Arrange
        var dto = new CreateReportDto { Delito = "Robo" };
        var userId = Guid.NewGuid();

        // Act
        var result = await _service.CreateReportAsync(dto, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Robo", result.Delito);
    }
}
```

### Integration Tests (Infrastructure Layer)

Prueban con base de datos real o TestContainers.

```csharp
public class ReportRepositoryTests : IClassFixture<DatabaseFixture>
{
    private readonly CeibaDbContext _context;

    public ReportRepositoryTests(DatabaseFixture fixture)
    {
        _context = fixture.CreateContext();
    }

    [Fact]
    public async Task CanSaveAndRetrieveReport()
    {
        // Arrange
        var report = new ReporteIncidencia { Delito = "Robo" };

        // Act
        _context.Reports.Add(report);
        await _context.SaveChangesAsync();

        var retrieved = await _context.Reports.FindAsync(report.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("Robo", retrieved.Delito);
    }
}
```

### Component Tests (Web Layer - bUnit)

Prueban componentes Blazor.

```csharp
public class ReportFormTests : TestContext
{
    [Fact]
    public void RendersFormFields()
    {
        // Arrange
        Services.AddScoped<IReportService>(_ => Mock.Of<IReportService>());

        // Act
        var cut = RenderComponent<ReportForm>();

        // Assert
        Assert.NotNull(cut.Find("#delito"));
        Assert.NotNull(cut.Find("#hechosReportados"));
    }

    [Fact]
    public void SubmitButton_DisabledWhenSaving()
    {
        // Arrange
        Services.AddScoped<IReportService>(_ => Mock.Of<IReportService>());
        var cut = RenderComponent<ReportForm>();

        // Act
        cut.Find("form").Submit();

        // Assert
        Assert.True(cut.Find("button[type=submit]").HasAttribute("disabled"));
    }
}
```

### E2E Tests (Playwright)

Prueban flujos completos de usuario.

```csharp
public class LoginFlowTests : PageTest
{
    [Test]
    public async Task UserCanLogin()
    {
        await Page.GotoAsync("https://localhost:5001/login");

        await Page.FillAsync("#email", "test@example.com");
        await Page.FillAsync("#password", "Password123!");
        await Page.ClickAsync("button[type=submit]");

        await Expect(Page).ToHaveURLAsync("https://localhost:5001/");
    }
}
```

## Convenciones de Nombrado

### Nombres de Tests

```csharp
// Patrón: MethodName_Scenario_ExpectedResult
[Fact]
public void CreateReport_WithValidData_ReturnsReport()

[Fact]
public async Task GetReportById_WhenNotFound_ThrowsNotFoundException()

[Fact]
public void Submit_WhenAlreadySubmitted_ThrowsInvalidOperationException()
```

### Clases de Test

```csharp
// Patrón: ClassNameTests
public class ReportServiceTests
public class ReporteIncidenciaTests
public class UserListComponentTests
```

## Ejecutar Tests

```bash
# Todos los tests
dotnet test

# Proyecto específico
dotnet test tests/Ceiba.Core.Tests

# Con filtro
dotnet test --filter "Category=Unit"
dotnet test --filter "FullyQualifiedName~ReportService"

# Con cobertura
dotnet test --collect:"XPlat Code Coverage"

# Verbose
dotnet test --logger "console;verbosity=detailed"
```

## Cobertura de Código

### Objetivos de Cobertura

| Capa | Objetivo |
|------|----------|
| Core | 90%+ |
| Application | 80%+ |
| Infrastructure | 70%+ |
| Web | Flujos críticos |

### Generar Reporte

```bash
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coveragereport
```

## Mocking

### Con Moq

```csharp
var mockService = new Mock<IReportService>();
mockService
    .Setup(s => s.GetReportByIdAsync(It.IsAny<int>()))
    .ReturnsAsync(new ReportDto { Id = 1 });
```

### Con NSubstitute (alternativa)

```csharp
var mockService = Substitute.For<IReportService>();
mockService
    .GetReportByIdAsync(Arg.Any<int>())
    .Returns(new ReportDto { Id = 1 });
```

## Assertions con FluentAssertions

```csharp
// Básicas
result.Should().NotBeNull();
result.Id.Should().Be(1);
result.Delito.Should().Be("Robo");

// Colecciones
reports.Should().HaveCount(5);
reports.Should().Contain(r => r.Delito == "Robo");

// Excepciones
action.Should().Throw<NotFoundException>()
    .WithMessage("*not found*");
```

## Próximos Pasos

- [[Dev Base de Datos|Modelo de base de datos]]
- [[Dev Guia Debugging|Debugging]]
- [[Dev Estandares Codigo|Estándares de código]]
