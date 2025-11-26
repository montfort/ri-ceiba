using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Ceiba.Integration.Tests;

/// <summary>
/// Contract tests for Report API endpoints
/// Validates API contracts defined in api-reports.yaml
/// </summary>
[Collection("Integration")]
public class ReportContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ReportContractTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    #region T021: Contract test for POST /api/reports

    [Fact(DisplayName = "T021: POST /api/reports should create report and return 201 with ReportResponse schema")]
    public async Task CreateReport_WithValidData_Returns201WithCorrectSchema()
    {
        // Arrange: Authenticate as CREADOR user
        var creadorUser = await AuthenticateAsCreadorAsync();

        var createRequest = new
        {
            tipoReporte = "A",
            datetimeHechos = DateTime.UtcNow.AddHours(-2),
            sexo = "Femenino",
            edad = 28,
            lgbtttiqPlus = false,
            situacionCalle = false,
            migrante = false,
            discapacidad = false,
            delito = "Violencia familiar",
            zonaId = 1,
            sectorId = 1,
            cuadranteId = 1,
            turnoCeiba = 1,
            tipoDeAtencion = "Presencial",
            tipoDeAccion = 1, // ATOS
            hechosReportados = "Descripción detallada de los hechos reportados para el incidente.",
            accionesRealizadas = "Acciones realizadas por el oficial para atender el caso.",
            traslados = 0, // Sin traslados
            observaciones = "Observaciones adicionales del caso"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/reports", createRequest);

        // Assert: Verify HTTP 201 Created
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Assert: Verify response schema matches ReportResponse
        var content = await response.Content.ReadAsStringAsync();
        var reportResponse = JsonSerializer.Deserialize<JsonElement>(content);

        reportResponse.GetProperty("id").GetInt32().Should().BeGreaterThan(0);
        reportResponse.GetProperty("tipoReporte").GetString().Should().Be("A");
        reportResponse.GetProperty("estado").GetInt32().Should().Be(0); // Borrador
        reportResponse.GetProperty("usuarioId").GetString().Should().NotBeNullOrEmpty();
        reportResponse.GetProperty("createdAt").GetString().Should().NotBeNullOrEmpty();
        reportResponse.GetProperty("datetimeHechos").GetString().Should().NotBeNullOrEmpty();
        reportResponse.GetProperty("sexo").GetString().Should().Be("Femenino");
        reportResponse.GetProperty("edad").GetInt32().Should().Be(28);
        reportResponse.GetProperty("delito").GetString().Should().Be("Violencia familiar");
        reportResponse.GetProperty("zona").GetProperty("id").GetInt32().Should().Be(1);
        reportResponse.GetProperty("sector").GetProperty("id").GetInt32().Should().Be(1);
        reportResponse.GetProperty("cuadrante").GetProperty("id").GetInt32().Should().Be(1);
        reportResponse.GetProperty("hechosReportados").GetString().Should().NotBeNullOrEmpty();
        reportResponse.GetProperty("accionesRealizadas").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact(DisplayName = "T021: POST /api/reports without authentication should return 401")]
    public async Task CreateReport_WithoutAuthentication_Returns401()
    {
        // Arrange
        var createRequest = new
        {
            tipoReporte = "A",
            datetimeHechos = DateTime.UtcNow,
            sexo = "Femenino",
            edad = 28,
            delito = "Test",
            zonaId = 1,
            sectorId = 1,
            cuadranteId = 1,
            turnoCeiba = 1,
            tipoDeAtencion = "Presencial",
            tipoDeAccion = 1,
            hechosReportados = "Test",
            accionesRealizadas = "Test",
            traslados = 0
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/reports", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "T021: POST /api/reports as non-CREADOR should return 403")]
    public async Task CreateReport_AsNonCreador_Returns403()
    {
        // Arrange: Authenticate as ADMIN (not CREADOR)
        var adminUser = await AuthenticateAsAdminAsync();

        var createRequest = new
        {
            tipoReporte = "A",
            datetimeHechos = DateTime.UtcNow,
            sexo = "Femenino",
            edad = 28,
            delito = "Test",
            zonaId = 1,
            sectorId = 1,
            cuadranteId = 1,
            turnoCeiba = 1,
            tipoDeAtencion = "Presencial",
            tipoDeAccion = 1,
            hechosReportados = "Test",
            accionesRealizadas = "Test",
            traslados = 0
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/reports", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region T022: Contract test for PUT /api/reports/{id}

    [Fact(DisplayName = "T022: PUT /api/reports/{id} should update draft and return 200")]
    public async Task UpdateReport_WithValidDraft_Returns200()
    {
        // Arrange: Create a draft report first
        var creadorUser = await AuthenticateAsCreadorAsync();
        var reportId = await CreateDraftReportAsync();

        var updateRequest = new
        {
            sexo = "Masculino",
            edad = 35,
            delito = "Robo con violencia",
            hechosReportados = "Hechos actualizados con más detalles sobre el incidente.",
            accionesRealizadas = "Acciones actualizadas con más información."
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/reports/{reportId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "T022: PUT /api/reports/{id} as CREADOR on submitted report should return 403")]
    public async Task UpdateReport_AsCreadorOnSubmittedReport_Returns403()
    {
        // Arrange: Create and submit a report
        var creadorUser = await AuthenticateAsCreadorAsync();
        var reportId = await CreateAndSubmitReportAsync();

        var updateRequest = new
        {
            sexo = "Masculino"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/reports/{reportId}", updateRequest);

        // Assert: CREADOR cannot edit submitted reports
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "T022: PUT /api/reports/{id} as REVISOR on any report should return 200")]
    public async Task UpdateReport_AsRevisorOnAnyReport_Returns200()
    {
        // Arrange: Create a submitted report as CREADOR
        var creadorUser = await AuthenticateAsCreadorAsync();
        var reportId = await CreateAndSubmitReportAsync();

        // Re-authenticate as REVISOR
        var revisorUser = await AuthenticateAsRevisorAsync();

        var updateRequest = new
        {
            sexo = "Masculino",
            observaciones = "Modificado por supervisor"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/reports/{reportId}", updateRequest);

        // Assert: REVISOR can edit any report
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region T023: Contract test for POST /api/reports/{id}/submit

    [Fact(DisplayName = "T023: POST /api/reports/{id}/submit should change estado to 1 and return 200")]
    public async Task SubmitReport_WithValidDraft_Returns200AndChangesEstado()
    {
        // Arrange: Create a draft report
        var creadorUser = await AuthenticateAsCreadorAsync();
        var reportId = await CreateDraftReportAsync();

        // Act
        var response = await _client.PostAsync($"/api/reports/{reportId}/submit", null);

        // Assert: Verify submission success
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify estado changed to 1 (Entregado)
        var getResponse = await _client.GetAsync($"/api/reports/{reportId}");
        var content = await getResponse.Content.ReadAsStringAsync();
        var report = JsonSerializer.Deserialize<JsonElement>(content);
        report.GetProperty("estado").GetInt32().Should().Be(1); // Entregado
    }

    [Fact(DisplayName = "T023: POST /api/reports/{id}/submit on already submitted report should return 400")]
    public async Task SubmitReport_OnAlreadySubmittedReport_Returns400()
    {
        // Arrange: Create and submit a report
        var creadorUser = await AuthenticateAsCreadorAsync();
        var reportId = await CreateAndSubmitReportAsync();

        // Act: Try to submit again
        var response = await _client.PostAsync($"/api/reports/{reportId}/submit", null);

        // Assert: Should fail because already submitted
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "T023: POST /api/reports/{id}/submit on another user's report should return 403")]
    public async Task SubmitReport_OnOtherUsersReport_Returns403()
    {
        // Arrange: Create report as first CREADOR
        var creador1 = await AuthenticateAsCreadorAsync();
        var reportId = await CreateDraftReportAsync();

        // Authenticate as different CREADOR
        var creador2 = await AuthenticateAsCreadorAsync("creador2@ceiba.local", "Creador2!");

        // Act: Try to submit other user's report
        var response = await _client.PostAsync($"/api/reports/{reportId}/submit", null);

        // Assert: Should be forbidden
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Helper Methods

    private async Task<string> AuthenticateAsCreadorAsync(
        string email = "creador@ceiba.local",
        string password = "Creador123!")
    {
        var loginRequest = new { email, password };
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        response.EnsureSuccessStatusCode();

        // Store authentication cookie for subsequent requests
        // Cookie is automatically managed by HttpClient
        return email;
    }

    private async Task<string> AuthenticateAsRevisorAsync(
        string email = "revisor@ceiba.local",
        string password = "Revisor123!")
    {
        var loginRequest = new { email, password };
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        response.EnsureSuccessStatusCode();
        return email;
    }

    private async Task<string> AuthenticateAsAdminAsync(
        string email = "admin@ceiba.local",
        string password = "Admin123!")
    {
        var loginRequest = new { email, password };
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        response.EnsureSuccessStatusCode();
        return email;
    }

    private async Task<int> CreateDraftReportAsync()
    {
        var createRequest = new
        {
            tipoReporte = "A",
            datetimeHechos = DateTime.UtcNow.AddHours(-2),
            sexo = "Femenino",
            edad = 28,
            lgbtttiqPlus = false,
            situacionCalle = false,
            migrante = false,
            discapacidad = false,
            delito = "Violencia familiar",
            zonaId = 1,
            sectorId = 1,
            cuadranteId = 1,
            turnoCeiba = 1,
            tipoDeAtencion = "Presencial",
            tipoDeAccion = 1,
            hechosReportados = "Descripción detallada de los hechos.",
            accionesRealizadas = "Acciones realizadas.",
            traslados = 0,
            observaciones = "Observaciones"
        };

        var response = await _client.PostAsJsonAsync("/api/reports", createRequest);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var report = JsonSerializer.Deserialize<JsonElement>(content);
        return report.GetProperty("id").GetInt32();
    }

    private async Task<int> CreateAndSubmitReportAsync()
    {
        var reportId = await CreateDraftReportAsync();
        var response = await _client.PostAsync($"/api/reports/{reportId}/submit", null);
        response.EnsureSuccessStatusCode();
        return reportId;
    }

    #endregion
}
