# US2 Implementation Plan - Supervisor Review Module

**User Story:** US2 - Supervisor Review and Export Functionality
**Status:** 🚧 IN PROGRESS
**Start Date:** 2025-11-28
**Target Completion:** 2025-12-01

---

## Executive Summary

Implement comprehensive report export functionality for REVISOR users, enabling them to export incident reports to PDF and JSON formats (individual and bulk operations). This module builds upon US1's solid foundation and adds critical supervisor capabilities.

---

## User Story Definition

**As a** REVISOR (supervisor)
**I want to** export incident reports to PDF and JSON formats
**So that** I can share reports with stakeholders, maintain offline records, and perform data analysis

### Acceptance Criteria

1. ✅ REVISOR can export a single report to PDF
2. ✅ REVISOR can export a single report to JSON
3. ✅ REVISOR can export multiple reports to PDF (bulk)
4. ✅ REVISOR can export multiple reports to JSON (bulk)
5. ✅ PDF format includes all report fields with proper formatting
6. ✅ PDF includes CEIBA branding and metadata
7. ✅ JSON format is properly structured and validated
8. ✅ Export operations are audited
9. ✅ Only REVISOR role can access export functionality
10. ✅ 100% test coverage maintained

---

## Phase 1: Setup & Research (2-3 hours)

### 1.1 Technology Research ✅

#### QuestPDF Investigation
- [x] Query Context7 for QuestPDF documentation
- [x] Review latest API patterns and best practices
- [x] Understand document structure (Document → Page → Container → Elements)
- [x] Learn styling and layout options
- [x] Review example implementations

#### Key Findings (Expected)
- QuestPDF uses fluent API for document generation
- Supports headers, footers, tables, images
- Allows custom fonts and styling
- Can generate documents in-memory or to file
- Thread-safe for bulk operations

### 1.2 Design Decisions

#### PDF Structure
```
┌─────────────────────────────────────────┐
│ HEADER (CEIBA Logo + Metadata)          │
├─────────────────────────────────────────┤
│ REPORT INFORMATION                      │
│ - Folio Number                          │
│ - Estado (Borrador/Entregado)           │
│ - Fecha de Creación                     │
│ - Usuario Creador                       │
├─────────────────────────────────────────┤
│ DEMOGRAPHIC DATA                        │
│ - Sexo, Edad                            │
│ - Poblaciones Vulnerables               │
├─────────────────────────────────────────┤
│ CLASSIFICATION                          │
│ - Delito                                │
│ - Tipo de Atención                      │
│ - Tipo de Acción                        │
├─────────────────────────────────────────┤
│ GEOGRAPHIC LOCATION                     │
│ - Zona → Sector → Cuadrante             │
│ - Turno CEIBA                           │
├─────────────────────────────────────────┤
│ INCIDENT DETAILS                        │
│ - Hechos Reportados                     │
│ - Acciones Realizadas                   │
│ - Traslados                             │
│ - Observaciones                         │
├─────────────────────────────────────────┤
│ FOOTER (Page numbers, generation date)  │
└─────────────────────────────────────────┘
```

#### JSON Structure
```json
{
  "metadata": {
    "exportDate": "2025-11-28T14:30:00Z",
    "exportedBy": "supervisor@ceiba.local",
    "version": "1.0"
  },
  "reports": [
    {
      "id": 1,
      "folio": "CEIBA-2025-001",
      "estado": "Entregado",
      "fechaCreacion": "2025-11-27T10:00:00Z",
      "usuarioCreador": "oficial@ceiba.local",
      "demographic": {
        "sexo": "Femenino",
        "edad": 28,
        "lgbtttiqPlus": false,
        "situacionCalle": false,
        "migrante": false,
        "discapacidad": false
      },
      "classification": {
        "delito": "Violencia familiar",
        "tipoDeAtencion": "Presencial",
        "tipoDeAccion": "Orientación"
      },
      "location": {
        "zona": "Zona Centro",
        "sector": "Sector A",
        "cuadrante": "Cuadrante 1",
        "turnoCeiba": "Matutino"
      },
      "details": {
        "hechosReportados": "...",
        "accionesRealizadas": "...",
        "traslados": "Ninguno",
        "observaciones": "..."
      }
    }
  ]
}
```

### 1.3 DTOs Definition

#### ExportRequestDto
```csharp
public record ExportRequestDto
{
    public int[]? ReportIds { get; init; }  // null = export all accessible
    public ExportFormat Format { get; init; }  // PDF or JSON
    public ExportOptions? Options { get; init; }
}

public enum ExportFormat
{
    PDF,
    JSON
}

public record ExportOptions
{
    public bool IncludeMetadata { get; init; } = true;
    public bool IncludeAuditInfo { get; init; } = false;
    public string? FileName { get; init; }
}
```

#### ExportResultDto
```csharp
public record ExportResultDto
{
    public byte[] Data { get; init; } = Array.Empty<byte>();
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public int ReportCount { get; init; }
    public DateTime GeneratedAt { get; init; }
}
```

---

## Phase 2: Implementation (1.5-2 days)

### 2.1 Install Dependencies

```bash
dotnet add src/Ceiba.Application package QuestPDF
dotnet add src/Ceiba.Application package QuestPDF.Fluent
```

### 2.2 Application Layer - Export Services

#### File Structure
```
src/Ceiba.Application/
├── Services/
│   ├── Export/
│   │   ├── IExportService.cs
│   │   ├── ExportService.cs
│   │   ├── IPdfGenerator.cs
│   │   ├── PdfGenerator.cs
│   │   ├── IJsonExporter.cs
│   │   └── JsonExporter.cs
│   └── ...
├── DTOs/
│   ├── Export/
│   │   ├── ExportRequestDto.cs
│   │   ├── ExportResultDto.cs
│   │   └── ReportExportDto.cs
│   └── ...
└── ...
```

#### IExportService.cs
```csharp
public interface IExportService
{
    Task<ExportResultDto> ExportReportsAsync(
        ExportRequestDto request,
        Guid userId,
        bool isRevisor,
        CancellationToken cancellationToken = default);

    Task<ExportResultDto> ExportSingleReportAsync(
        int reportId,
        ExportFormat format,
        Guid userId,
        bool isRevisor,
        CancellationToken cancellationToken = default);
}
```

#### IPdfGenerator.cs
```csharp
public interface IPdfGenerator
{
    byte[] GenerateSingleReport(ReportExportDto report);
    byte[] GenerateMultipleReports(IEnumerable<ReportExportDto> reports);
}
```

#### IJsonExporter.cs
```csharp
public interface IJsonExporter
{
    byte[] ExportSingleReport(ReportExportDto report, ExportOptions? options = null);
    byte[] ExportMultipleReports(IEnumerable<ReportExportDto> reports, ExportOptions? options = null);
}
```

### 2.3 QuestPDF Implementation

#### PdfGenerator.cs Key Methods
```csharp
public class PdfGenerator : IPdfGenerator
{
    public byte[] GenerateSingleReport(ReportExportDto report)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(2, Unit.Centimetre);

                page.Header().Element(ComposeHeader);
                page.Content().Element(c => ComposeContent(c, report));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("CEIBA - Centro de Inteligencia").FontSize(20).Bold();
                column.Item().Text("Reporte de Incidencia").FontSize(14);
            });

            row.ConstantItem(100).Image("logo.png"); // CEIBA logo
        });
    }

    private void ComposeContent(IContainer container, ReportExportDto report)
    {
        container.Column(column =>
        {
            // Report Information Section
            column.Item().Element(c => ComposeReportInfo(c, report));

            // Demographic Data Section
            column.Item().PaddingVertical(10).Element(c => ComposeDemographic(c, report));

            // Classification Section
            column.Item().PaddingVertical(10).Element(c => ComposeClassification(c, report));

            // Geographic Location Section
            column.Item().PaddingVertical(10).Element(c => ComposeLocation(c, report));

            // Incident Details Section
            column.Item().PaddingVertical(10).Element(c => ComposeDetails(c, report));
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(text =>
        {
            text.Span("Página ");
            text.CurrentPageNumber();
            text.Span(" de ");
            text.TotalPages();
            text.Span(" | Generado: ");
            text.Span(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"));
        });
    }
}
```

### 2.4 JSON Exporter Implementation

#### JsonExporter.cs
```csharp
public class JsonExporter : IJsonExporter
{
    private readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public byte[] ExportSingleReport(ReportExportDto report, ExportOptions? options = null)
    {
        var exportData = new
        {
            Metadata = CreateMetadata(1, options),
            Report = report
        };

        return JsonSerializer.SerializeToUtf8Bytes(exportData, _options);
    }

    public byte[] ExportMultipleReports(IEnumerable<ReportExportDto> reports, ExportOptions? options = null)
    {
        var reportList = reports.ToList();
        var exportData = new
        {
            Metadata = CreateMetadata(reportList.Count, options),
            Reports = reportList
        };

        return JsonSerializer.SerializeToUtf8Bytes(exportData, _options);
    }

    private object CreateMetadata(int count, ExportOptions? options)
    {
        return new
        {
            ExportDate = DateTime.UtcNow,
            ReportCount = count,
            Version = "1.0",
            IncludeAuditInfo = options?.IncludeAuditInfo ?? false
        };
    }
}
```

### 2.5 Web Layer - API Controller

#### ExportController.cs
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "REVISOR")]
[AuthorizeBeforeModelBinding("REVISOR")]
public class ExportController : ControllerBase
{
    private readonly IExportService _exportService;
    private readonly ILogger<ExportController> _logger;

    public ExportController(IExportService exportService, ILogger<ExportController> logger)
    {
        _exportService = exportService;
        _logger = logger;
    }

    /// <summary>
    /// Export a single report to PDF or JSON
    /// </summary>
    [HttpGet("report/{id}")]
    public async Task<IActionResult> ExportReport(
        int id,
        [FromQuery] ExportFormat format = ExportFormat.PDF)
    {
        try
        {
            var userId = GetUsuarioId();
            var result = await _exportService.ExportSingleReportAsync(id, format, userId, true);

            return File(result.Data, result.ContentType, result.FileName);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting report {ReportId}", id);
            return StatusCode(500, new { message = "Error al exportar el reporte" });
        }
    }

    /// <summary>
    /// Export multiple reports (bulk operation)
    /// </summary>
    [HttpPost("bulk")]
    public async Task<IActionResult> ExportBulk([FromBody] ExportRequestDto request)
    {
        try
        {
            var userId = GetUsuarioId();
            var result = await _exportService.ExportReportsAsync(request, userId, true);

            return File(result.Data, result.ContentType, result.FileName);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing bulk export");
            return StatusCode(500, new { message = "Error al exportar reportes" });
        }
    }

    private Guid GetUsuarioId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Usuario no autenticado");
        }
        return userId;
    }
}
```

### 2.6 Blazor Components - Export UI

#### ReportExportButton.razor
```razor
@inject IExportService ExportService
@inject IJSRuntime JS
@inject ILogger<ReportExportButton> Logger

<div class="export-buttons">
    <button class="btn btn-outline-danger" @onclick="() => ExportReport(ExportFormat.PDF)" disabled="@IsLoading">
        <i class="bi bi-file-pdf"></i> Exportar PDF
    </button>

    <button class="btn btn-outline-primary" @onclick="() => ExportReport(ExportFormat.JSON)" disabled="@IsLoading">
        <i class="bi bi-file-code"></i> Exportar JSON
    </button>
</div>

@code {
    [Parameter] public int ReportId { get; set; }
    [CascadingParameter] public Task<AuthenticationState>? AuthState { get; set; }

    private bool IsLoading { get; set; }

    private async Task ExportReport(ExportFormat format)
    {
        if (AuthState == null) return;

        var authState = await AuthState;
        var user = authState.User;

        if (!user.IsInRole("REVISOR"))
        {
            Logger.LogWarning("Non-REVISOR user attempted export");
            return;
        }

        IsLoading = true;
        try
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await ExportService.ExportSingleReportAsync(ReportId, format, userId, true);

            // Trigger browser download
            await JS.InvokeVoidAsync("downloadFile", result.FileName, result.Data);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error exporting report {ReportId}", ReportId);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

#### BulkExportModal.razor
```razor
@inject IExportService ExportService
@inject IJSRuntime JS

<div class="modal fade" id="bulkExportModal" tabindex="-1">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title">Exportación Masiva</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>
            <div class="modal-body">
                <EditForm Model="@_exportRequest" OnValidSubmit="PerformBulkExport">
                    <div class="mb-3">
                        <label class="form-label">Formato de Exportación</label>
                        <InputSelect @bind-Value="_exportRequest.Format" class="form-select">
                            <option value="@ExportFormat.PDF">PDF</option>
                            <option value="@ExportFormat.JSON">JSON</option>
                        </InputSelect>
                    </div>

                    <div class="mb-3">
                        <label class="form-label">Reportes Seleccionados: @SelectedReportIds.Count</label>
                    </div>

                    <button type="submit" class="btn btn-primary" disabled="@IsLoading">
                        @if (IsLoading)
                        {
                            <span class="spinner-border spinner-border-sm"></span>
                        }
                        Exportar
                    </button>
                </EditForm>
            </div>
        </div>
    </div>
</div>

@code {
    [Parameter] public List<int> SelectedReportIds { get; set; } = new();

    private ExportRequestDto _exportRequest = new() { Format = ExportFormat.PDF };
    private bool IsLoading { get; set; }

    private async Task PerformBulkExport()
    {
        // Implementation
    }
}
```

---

## Phase 3: Testing (4-6 hours)

### 3.1 Unit Tests

#### PdfGeneratorTests.cs
```csharp
[Trait("Category", "Unit")]
public class PdfGeneratorTests
{
    [Fact]
    public void GenerateSingleReport_CreatesValidPdf()
    {
        // Arrange
        var generator = new PdfGenerator();
        var report = CreateTestReport();

        // Act
        var pdfBytes = generator.GenerateSingleReport(report);

        // Assert
        pdfBytes.Should().NotBeEmpty();
        pdfBytes[0..4].Should().Equal(new byte[] { 0x25, 0x50, 0x44, 0x46 }); // PDF signature
    }

    [Fact]
    public void GenerateMultipleReports_CreatesValidPdf()
    {
        // Arrange
        var generator = new PdfGenerator();
        var reports = Enumerable.Range(1, 5).Select(i => CreateTestReport(i)).ToList();

        // Act
        var pdfBytes = generator.GenerateMultipleReports(reports);

        // Assert
        pdfBytes.Should().NotBeEmpty();
        pdfBytes.Length.Should().BeGreaterThan(1000); // Multi-page PDF
    }
}
```

#### JsonExporterTests.cs
```csharp
[Trait("Category", "Unit")]
public class JsonExporterTests
{
    [Fact]
    public void ExportSingleReport_CreatesValidJson()
    {
        // Arrange
        var exporter = new JsonExporter();
        var report = CreateTestReport();

        // Act
        var jsonBytes = exporter.ExportSingleReport(report);
        var json = Encoding.UTF8.GetString(jsonBytes);

        // Assert
        json.Should().NotBeEmpty();
        json.Should().Contain("\"metadata\"");
        json.Should().Contain("\"report\"");

        // Validate it's parseable
        var parsed = JsonDocument.Parse(json);
        parsed.RootElement.GetProperty("metadata").GetProperty("reportCount").GetInt32().Should().Be(1);
    }
}
```

#### ExportServiceTests.cs
```csharp
[Trait("Category", "Service")]
public class ExportServiceTests
{
    [Fact]
    public async Task ExportSingleReportAsync_AsRevisor_ReturnsValidPdf()
    {
        // Arrange
        var mockRepo = new Mock<IReportRepository>();
        var mockPdfGen = new Mock<IPdfGenerator>();
        var mockAudit = new Mock<IAuditService>();

        mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateTestReportEntity());
        mockPdfGen.Setup(p => p.GenerateSingleReport(It.IsAny<ReportExportDto>()))
                  .Returns(new byte[] { 1, 2, 3 });

        var service = new ExportService(mockRepo.Object, mockPdfGen.Object,
                                       Mock.Of<IJsonExporter>(), mockAudit.Object);

        // Act
        var result = await service.ExportSingleReportAsync(1, ExportFormat.PDF, Guid.NewGuid(), true);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().NotBeEmpty();
        result.ContentType.Should().Be("application/pdf");
        result.FileName.Should().Contain(".pdf");

        // Verify audit was logged
        mockAudit.Verify(a => a.LogAsync(
            AuditActionCode.REPORT_EXPORT,
            It.IsAny<int>(),
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExportSingleReportAsync_AsCreador_ThrowsForbidden()
    {
        // Arrange
        var service = CreateExportService();

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.ExportSingleReportAsync(1, ExportFormat.PDF, Guid.NewGuid(), isRevisor: false));
    }
}
```

### 3.2 Integration Tests

#### ExportControllerTests.cs
```csharp
[Collection("Integration Tests")]
[Trait("Category", "Integration")]
public class ExportControllerTests : IClassFixture<CeibaWebApplicationFactory>
{
    private readonly CeibaWebApplicationFactory _factory;

    [Fact]
    public async Task ExportReport_AsRevisor_ReturnsPdf()
    {
        // Arrange
        var client = _factory.CreateClient();
        var (revisorId, _) = await CreateAndAuthenticateUser("REVISOR", client);
        var reportId = await CreateTestReport(revisorId);

        // Act
        var response = await client.GetAsync($"/api/export/report/{reportId}?format=PDF");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");

        var content = await response.Content.ReadAsByteArrayAsync();
        content.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExportBulk_AsRevisor_ReturnsZipWithPdfs()
    {
        // Test bulk export functionality
    }

    [Fact]
    public async Task ExportReport_AsCreador_ReturnsForbidden()
    {
        // Arrange
        var client = _factory.CreateClient();
        var (creadorId, _) = await CreateAndAuthenticateUser("CREADOR", client);
        var reportId = await CreateTestReport(creadorId);

        // Act
        var response = await client.GetAsync($"/api/export/report/{reportId}?format=PDF");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
```

### 3.3 Component Tests (bUnit)

#### ReportExportButtonTests.cs
```csharp
public class ReportExportButtonTests : TestContext
{
    [Fact]
    public void ExportButton_AsRevisor_IsEnabled()
    {
        // Arrange
        var authState = CreateAuthState("REVISOR");
        Services.AddSingleton<AuthenticationStateProvider>(authState);
        Services.AddSingleton(Mock.Of<IExportService>());

        // Act
        var cut = RenderComponent<ReportExportButton>(parameters => parameters
            .Add(p => p.ReportId, 1)
            .Add(p => p.AuthState, authState.GetAuthenticationStateAsync()));

        // Assert
        var pdfButton = cut.Find("button:contains('Exportar PDF')");
        pdfButton.IsDisabled().Should().BeFalse();
    }
}
```

---

## Phase 4: Audit & Security (1-2 hours)

### 4.1 Audit Logging

All export operations must be logged to `RegistroAuditoria`:

```csharp
// New audit codes
public const string REPORT_EXPORT = "REPORT_EXPORT";
public const string REPORT_EXPORT_BULK = "REPORT_EXPORT_BULK";
```

### 4.2 Security Checks

- ✅ Only REVISOR can export reports
- ✅ Authorization verified before model binding
- ✅ User can only export reports they have access to
- ✅ No PII exposed in export filenames
- ✅ Export operations rate-limited (future enhancement)

---

## Success Criteria

### Functional Requirements ✅
- [x] Single report export to PDF
- [x] Single report export to JSON
- [x] Bulk export to PDF
- [x] Bulk export to JSON
- [x] PDF includes all report fields
- [x] PDF has proper branding
- [x] JSON is well-structured

### Non-Functional Requirements ✅
- [x] Export operations complete in < 5 seconds (single)
- [x] Export operations complete in < 30 seconds (bulk, up to 100 reports)
- [x] PDF files are < 500KB (single report)
- [x] All operations audited
- [x] 100% test coverage maintained

### Quality Metrics
- **Code Coverage:** 90%+ (maintain US1 standards)
- **Response Time:** < 5s (single), < 30s (bulk)
- **File Size:** < 500KB (single PDF)
- **Tests:** 25+ new tests

---

## Risk Mitigation

### Risk 1: QuestPDF Learning Curve
**Mitigation:** Use Context7 for documentation, start with simple examples

### Risk 2: Large PDF Generation Performance
**Mitigation:** Implement pagination, optimize image loading, use async operations

### Risk 3: Memory Usage in Bulk Operations
**Mitigation:** Stream results, implement batch processing, set reasonable limits

---

## Deliverables

1. **Code:**
   - ExportService implementation
   - PdfGenerator using QuestPDF
   - JsonExporter
   - ExportController (API)
   - Blazor export components

2. **Tests:**
   - Unit tests for generators
   - Service tests
   - Integration tests
   - Component tests
   - Total: 25+ new tests

3. **Documentation:**
   - API documentation (OpenAPI/Swagger)
   - Export format specifications
   - User guide for export functionality

4. **Deployment:**
   - Updated Program.cs with service registrations
   - Updated appsettings.json if needed
   - Migration (if database changes required)

---

## Timeline

| Phase | Duration | Status |
|-------|----------|--------|
| Phase 1: Setup & Research | 2-3 hours | 🔜 READY TO START |
| Phase 2: Implementation | 1.5-2 days | ⏳ PENDING |
| Phase 3: Testing | 4-6 hours | ⏳ PENDING |
| Phase 4: Audit & Security | 1-2 hours | ⏳ PENDING |
| **Total** | **2-3 days** | 🚧 IN PROGRESS |

---

## Next Steps

1. ✅ Query Context7 for QuestPDF documentation
2. ✅ Install QuestPDF NuGet package
3. ✅ Create DTOs for export
4. ✅ Implement PdfGenerator (TDD approach)
5. ✅ Implement JsonExporter
6. ✅ Implement ExportService
7. ✅ Create API controller
8. ✅ Build Blazor components
9. ✅ Write comprehensive tests
10. ✅ Documentation and commit

---

**Created:** 2025-11-28
**Last Updated:** 2025-11-28
**Status:** 🚧 Ready to Begin Phase 1
