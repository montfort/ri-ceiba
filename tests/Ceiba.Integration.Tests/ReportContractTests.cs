using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Ceiba.Integration.Tests;

/// <summary>
/// Contract tests for Report API endpoints (MVP - Simplified)
/// Validates that the application starts correctly and endpoints are configured
/// Full authentication testing deferred to post-MVP
/// </summary>
[Collection("Integration")]
public class ReportContractTests : IClassFixture<CeibaWebApplicationFactory>
{
    private readonly CeibaWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ReportContractTests(CeibaWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    #region MVP: Application Startup and Basic Endpoint Tests

    [Fact(DisplayName = "MVP: Application should start successfully with in-memory database")]
    public void Application_ShouldStartSuccessfully()
    {
        // Arrange & Act: Factory creates client (application must start)

        // Assert: Client created successfully
        _client.Should().NotBeNull();
        _client.BaseAddress.Should().NotBeNull();
    }

    [Fact(DisplayName = "MVP: POST /api/reports without authentication should return 401 Unauthorized")]
    public async Task PostReports_WithoutAuth_Returns401()
    {
        // Arrange
        var requestBody = new
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
        var response = await _client.PostAsJsonAsync("/api/reports", requestBody);

        // Assert: Should require authentication
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "MVP: GET /api/reports without authentication should return 401 Unauthorized")]
    public async Task GetReports_WithoutAuth_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/reports");

        // Assert: Should require authentication
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "MVP: PUT /api/reports/{id} without authentication should return 401 Unauthorized")]
    public async Task PutReport_WithoutAuth_Returns401()
    {
        // Arrange
        var requestBody = new { sexo = "Masculino" };

        // Act
        var response = await _client.PutAsJsonAsync("/api/reports/1", requestBody);

        // Assert: Should require authentication
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "MVP: POST /api/reports/{id}/submit without authentication should return 401 Unauthorized")]
    public async Task SubmitReport_WithoutAuth_Returns401()
    {
        // Act
        var response = await _client.PostAsync("/api/reports/1/submit", null);

        // Assert: Should require authentication
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "MVP: Database catalogs should be seeded correctly")]
    public void DatabaseCatalogs_ShouldBeSeeded()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Ceiba.Infrastructure.Data.CeibaDbContext>();

        // Act & Assert: Verify catalog data exists
        db.Zonas.Count().Should().BeGreaterThan(0);
        db.Sectores.Count().Should().BeGreaterThan(0);
        db.Cuadrantes.Count().Should().BeGreaterThan(0);
        db.CatalogosSugerencia.Count().Should().BeGreaterThan(0);
    }

    #endregion

    #region Post-MVP: Full Authentication Tests (Deferred)

    // NOTE: The following tests require full ASP.NET Identity configuration with test users
    // These are deferred to post-MVP and will be implemented when authentication infrastructure
    // is fully configured for integration testing

    // [Fact(Skip = "Post-MVP: Requires full authentication setup")]
    // public async Task CreateReport_AsCreador_Returns201WithCorrectSchema() { }

    // [Fact(Skip = "Post-MVP: Requires full authentication setup")]
    // public async Task UpdateReport_AsCreadorOnOwnDraft_Returns200() { }

    // [Fact(Skip = "Post-MVP: Requires full authentication setup")]
    // public async Task UpdateReport_AsCreadorOnSubmittedReport_Returns403() { }

    // [Fact(Skip = "Post-MVP: Requires full authentication setup")]
    // public async Task UpdateReport_AsRevisorOnAnyReport_Returns200() { }

    // [Fact(Skip = "Post-MVP: Requires full authentication setup")]
    // public async Task SubmitReport_AsCreadorOnOwnDraft_Returns200() { }

    // [Fact(Skip = "Post-MVP: Requires full authentication setup")]
    // public async Task SubmitReport_OnAlreadySubmittedReport_Returns400() { }

    // [Fact(Skip = "Post-MVP: Requires full authentication setup")]
    // public async Task SubmitReport_OnOtherUserReport_Returns403() { }

    #endregion
}
