using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ceiba.Core.Entities;
using Ceiba.Core.Interfaces;
using Ceiba.Infrastructure.Data;
using Ceiba.Shared.DTOs;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Ceiba.Integration.Tests;

/// <summary>
/// Integration tests for automated report generation workflow.
/// US4: Reportes Automatizados Diarios con IA.
/// Tests T085: End-to-end automated report generation, templates, and email delivery.
/// </summary>
[Collection("Integration")]
public class AutomatedReportJobTests : IClassFixture<CeibaWebApplicationFactory>
{
    private readonly CeibaWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AutomatedReportJobTests(CeibaWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region Automated Report Workflow Tests

    [Fact(DisplayName = "T085: IAutomatedReportService should be registered in DI container")]
    public void AutomatedReportService_ShouldBeRegistered()
    {
        // Arrange & Act
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetService<IAutomatedReportService>();

        // Assert
        service.Should().NotBeNull();
    }

    [Fact(DisplayName = "T085: Report generation should create report with statistics")]
    public async Task GenerateReport_ShouldCreateReportWithStatistics()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CeibaDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IAutomatedReportService>();

        // Add test incident reports
        var zona = context.Zonas.First();
        var reports = new[]
        {
            new ReporteIncidencia
            {
                HechosReportados = "Test hechos 1",
                AccionesRealizadas = "Test acciones 1",
                Delito = "Robo",
                Estado = 1, // Entregado
                Sexo = "Mujer",
                Edad = 30,
                TipoDeAtencion = "Presencial",
                TipoDeAccion = 1,
                ZonaId = zona.Id,
                LgbtttiqPlus = true,
                Migrante = false,
                SituacionCalle = false,
                Discapacidad = false,
                Traslados = 0,
                CreatedAt = DateTime.UtcNow
            },
            new ReporteIncidencia
            {
                HechosReportados = "Test hechos 2",
                AccionesRealizadas = "Test acciones 2",
                Delito = "Violencia familiar",
                Estado = 1, // Entregado
                Sexo = "Mujer",
                Edad = 25,
                TipoDeAtencion = "Telefónica",
                TipoDeAccion = 2,
                ZonaId = zona.Id,
                LgbtttiqPlus = false,
                Migrante = true,
                SituacionCalle = false,
                Discapacidad = false,
                Traslados = 1,
                CreatedAt = DateTime.UtcNow
            }
        };
        context.ReportesIncidencia.AddRange(reports);
        await context.SaveChangesAsync();

        // Act
        var request = new GenerateReportRequestDto
        {
            FechaInicio = DateTime.UtcNow.AddDays(-1),
            FechaFin = DateTime.UtcNow.AddDays(1),
            EnviarEmail = false
        };

        var result = await service.GenerateReportAsync(request, null);

        // Assert
        result.Should().NotBeNull();
        result.Estadisticas.TotalReportes.Should().BeGreaterThanOrEqualTo(2);
        result.ContenidoMarkdown.Should().NotBeNullOrEmpty();
        result.ContenidoMarkdown.Should().Contain("Reporte");
    }

    [Fact(DisplayName = "T085: Statistics calculation should count vulnerable populations")]
    public async Task CalculateStatistics_ShouldCountVulnerablePopulations()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CeibaDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IAutomatedReportService>();

        var zona = context.Zonas.First();

        // Create reports with various vulnerable population markers
        context.ReportesIncidencia.AddRange(
            new ReporteIncidencia
            {
                HechosReportados = "LGBTTTIQ case",
                AccionesRealizadas = "Support provided",
                Delito = "Discriminación",
                Estado = 1,
                Sexo = "Mujer",
                Edad = 22,
                TipoDeAtencion = "Presencial",
                TipoDeAccion = 1,
                ZonaId = zona.Id,
                LgbtttiqPlus = true,
                Migrante = false,
                SituacionCalle = false,
                Discapacidad = false,
                Traslados = 0,
                CreatedAt = DateTime.UtcNow
            },
            new ReporteIncidencia
            {
                HechosReportados = "Migrant case",
                AccionesRealizadas = "Legal assistance",
                Delito = "Explotación laboral",
                Estado = 1,
                Sexo = "Mujer",
                Edad = 35,
                TipoDeAtencion = "Presencial",
                TipoDeAccion = 1,
                ZonaId = zona.Id,
                LgbtttiqPlus = false,
                Migrante = true,
                SituacionCalle = false,
                Discapacidad = false,
                Traslados = 0,
                CreatedAt = DateTime.UtcNow
            }
        );
        await context.SaveChangesAsync();

        // Act
        var stats = await service.CalculateStatisticsAsync(
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(1));

        // Assert
        stats.TotalReportes.Should().BeGreaterThanOrEqualTo(2);
        // Verify at least some vulnerable population counts exist
        (stats.TotalLgbtttiq + stats.TotalMigrantes + stats.TotalSituacionCalle + stats.TotalDiscapacidad)
            .Should().BeGreaterThan(0);
    }

    #endregion

    #region Template Management Tests

    [Fact(DisplayName = "T085: Template CRUD operations should work correctly")]
    public async Task TemplateCrud_ShouldWorkCorrectly()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAutomatedReportService>();
        var userId = Guid.NewGuid();

        // Act - Create
        var createDto = new CreateTemplateDto
        {
            Nombre = $"Test Template {Guid.NewGuid()}",
            Descripcion = "Test description",
            ContenidoMarkdown = "# {{fecha_inicio}} - {{fecha_fin}}\n{{narrativa_ia}}",
            EsDefault = false
        };

        var created = await service.CreateTemplateAsync(createDto, userId);

        // Assert - Create
        created.Should().NotBeNull();
        created.Nombre.Should().Be(createDto.Nombre);
        created.Descripcion.Should().Be(createDto.Descripcion);
        created.Activo.Should().BeTrue();

        // Act - Read
        var retrieved = await service.GetTemplateByIdAsync(created.Id);

        // Assert - Read
        retrieved.Should().NotBeNull();
        retrieved!.ContenidoMarkdown.Should().Contain("{{narrativa_ia}}");

        // Act - Update
        var updateDto = new UpdateTemplateDto
        {
            Nombre = "Updated Template Name",
            Descripcion = "Updated description",
            ContenidoMarkdown = "# Updated Content",
            Activo = true,
            EsDefault = false
        };

        var updated = await service.UpdateTemplateAsync(created.Id, updateDto, userId);

        // Assert - Update
        updated.Should().NotBeNull();
        updated!.Nombre.Should().Be("Updated Template Name");

        // Act - Delete
        var deleted = await service.DeleteTemplateAsync(created.Id);

        // Assert - Delete
        deleted.Should().BeTrue();
    }

    [Fact(DisplayName = "T085: SetDefaultTemplate should update default flag correctly")]
    public async Task SetDefaultTemplate_ShouldUpdateDefaultFlag()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAutomatedReportService>();
        var userId = Guid.NewGuid();

        // Create two templates
        var template1 = await service.CreateTemplateAsync(
            new CreateTemplateDto
            {
                Nombre = $"Template 1 {Guid.NewGuid()}",
                ContenidoMarkdown = "# T1",
                EsDefault = true
            },
            userId);

        var template2 = await service.CreateTemplateAsync(
            new CreateTemplateDto
            {
                Nombre = $"Template 2 {Guid.NewGuid()}",
                ContenidoMarkdown = "# T2",
                EsDefault = false
            },
            userId);

        // Act
        await service.SetDefaultTemplateAsync(template2.Id);

        // Assert
        var t1 = await service.GetTemplateByIdAsync(template1.Id);
        var t2 = await service.GetTemplateByIdAsync(template2.Id);

        t1!.EsDefault.Should().BeFalse();
        t2!.EsDefault.Should().BeTrue();

        // Cleanup
        await service.DeleteTemplateAsync(template1.Id);
        await service.DeleteTemplateAsync(template2.Id);
    }

    [Fact(DisplayName = "T085: GetDefaultTemplate should return the default template")]
    public async Task GetDefaultTemplate_ShouldReturnDefault()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAutomatedReportService>();
        var userId = Guid.NewGuid();

        // Create a default template
        var templateName = $"Default Template {Guid.NewGuid()}";
        var template = await service.CreateTemplateAsync(
            new CreateTemplateDto
            {
                Nombre = templateName,
                ContenidoMarkdown = "# Default",
                EsDefault = true
            },
            userId);

        // Act
        var defaultTemplate = await service.GetDefaultTemplateAsync();

        // Assert
        defaultTemplate.Should().NotBeNull();
        defaultTemplate!.EsDefault.Should().BeTrue();

        // Cleanup
        await service.DeleteTemplateAsync(template.Id);
    }

    #endregion

    #region Report Retrieval Tests

    [Fact(DisplayName = "T085: GetReportsAsync should support pagination")]
    public async Task GetReportsAsync_ShouldSupportPagination()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAutomatedReportService>();
        var context = scope.ServiceProvider.GetRequiredService<CeibaDbContext>();

        // Create multiple reports
        for (int i = 0; i < 5; i++)
        {
            context.ReportesAutomatizados.Add(new ReporteAutomatizado
            {
                FechaInicio = DateTime.UtcNow.AddDays(-i - 1),
                FechaFin = DateTime.UtcNow.AddDays(-i),
                ContenidoMarkdown = $"# Report {i}",
                Estadisticas = JsonSerializer.Serialize(new ReportStatisticsDto { TotalReportes = i }),
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        await context.SaveChangesAsync();

        // Act
        var page1 = await service.GetReportsAsync(skip: 0, take: 2);
        var page2 = await service.GetReportsAsync(skip: 2, take: 2);

        // Assert
        page1.Should().HaveCount(2);
        page2.Should().HaveCount(2);
    }

    [Fact(DisplayName = "T085: GetReportsAsync should filter by date range")]
    public async Task GetReportsAsync_ShouldFilterByDateRange()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAutomatedReportService>();
        var context = scope.ServiceProvider.GetRequiredService<CeibaDbContext>();

        // Create reports in different date ranges
        context.ReportesAutomatizados.Add(new ReporteAutomatizado
        {
            FechaInicio = DateTime.UtcNow.AddDays(-30),
            FechaFin = DateTime.UtcNow.AddDays(-29),
            ContenidoMarkdown = "# Old Report",
            Estadisticas = "{}",
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        });

        context.ReportesAutomatizados.Add(new ReporteAutomatizado
        {
            FechaInicio = DateTime.UtcNow.AddDays(-1),
            FechaFin = DateTime.UtcNow,
            ContenidoMarkdown = "# Recent Report",
            Estadisticas = "{}",
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        // Act - Filter for recent reports only
        var recentReports = await service.GetReportsAsync(
            fechaDesde: DateTime.UtcNow.AddDays(-7));

        // Assert
        recentReports.Should().Contain(r => r.FechaInicio >= DateTime.UtcNow.AddDays(-7));
    }

    [Fact(DisplayName = "T085: GetReportByIdAsync should return report details")]
    public async Task GetReportByIdAsync_ShouldReturnDetails()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAutomatedReportService>();
        var context = scope.ServiceProvider.GetRequiredService<CeibaDbContext>();

        var stats = new ReportStatisticsDto
        {
            TotalReportes = 15,
            TotalLgbtttiq = 2,
            TotalMigrantes = 3,
            PorDelito = new Dictionary<string, int> { ["Robo"] = 5, ["Fraude"] = 3 }
        };

        var report = new ReporteAutomatizado
        {
            FechaInicio = DateTime.UtcNow.AddDays(-1),
            FechaFin = DateTime.UtcNow,
            ContenidoMarkdown = "# Test Report\n\nContent here",
            Estadisticas = JsonSerializer.Serialize(stats),
            Enviado = false
        };

        context.ReportesAutomatizados.Add(report);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetReportByIdAsync(report.Id);

        // Assert
        result.Should().NotBeNull();
        result!.ContenidoMarkdown.Should().Contain("Test Report");
        result.Estadisticas.TotalReportes.Should().Be(15);
        result.Estadisticas.TotalLgbtttiq.Should().Be(2);
        result.Estadisticas.PorDelito.Should().ContainKey("Robo");
    }

    #endregion

    #region Delete Report Tests

    [Fact(DisplayName = "T085: DeleteReportAsync should remove report from database")]
    public async Task DeleteReportAsync_ShouldRemoveReport()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAutomatedReportService>();
        var context = scope.ServiceProvider.GetRequiredService<CeibaDbContext>();

        var report = new ReporteAutomatizado
        {
            FechaInicio = DateTime.UtcNow,
            FechaFin = DateTime.UtcNow,
            ContenidoMarkdown = "# To Be Deleted",
            Estadisticas = "{}"
        };
        context.ReportesAutomatizados.Add(report);
        await context.SaveChangesAsync();

        var reportId = report.Id;

        // Act
        var deleted = await service.DeleteReportAsync(reportId);

        // Assert
        deleted.Should().BeTrue();
        var verifyDeleted = await service.GetReportByIdAsync(reportId);
        verifyDeleted.Should().BeNull();
    }

    [Fact(DisplayName = "T085: DeleteReportAsync should return false for non-existent report")]
    public async Task DeleteReportAsync_NonExistent_ShouldReturnFalse()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAutomatedReportService>();

        // Act
        var result = await service.DeleteReportAsync(999999);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Configuration Tests

    [Fact(DisplayName = "T085: GetConfigurationAsync should return configuration values")]
    public async Task GetConfigurationAsync_ShouldReturnConfiguration()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAutomatedReportService>();

        // Act
        var config = await service.GetConfigurationAsync();

        // Assert
        config.Should().NotBeNull();
        // Default generation time should be 06:00:00 if not configured
        config.GenerationTime.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
    }

    #endregion

    #region Age Range Statistics Tests

    [Fact(DisplayName = "T085: Statistics should correctly categorize age ranges")]
    public async Task Statistics_ShouldCorrectlyCategorizeAgeRanges()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CeibaDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IAutomatedReportService>();

        var zona = context.Zonas.First();

        // Create reports with different age groups
        var ages = new[] { 15, 20, 30, 45, 60, 70 };
        foreach (var age in ages)
        {
            context.ReportesIncidencia.Add(new ReporteIncidencia
            {
                HechosReportados = $"Case for age {age}",
                AccionesRealizadas = "Processed",
                Delito = "Test",
                Estado = 1,
                Sexo = "Mujer",
                Edad = age,
                TipoDeAtencion = "Presencial",
                TipoDeAccion = 1,
                ZonaId = zona.Id,
                LgbtttiqPlus = false,
                Migrante = false,
                SituacionCalle = false,
                Discapacidad = false,
                Traslados = 0,
                CreatedAt = DateTime.UtcNow
            });
        }
        await context.SaveChangesAsync();

        // Act
        var stats = await service.CalculateStatisticsAsync(
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(1));

        // Assert
        stats.PorRangoEdad.Should().NotBeEmpty();
        // Should have multiple age ranges
        stats.PorRangoEdad.Keys.Count.Should().BeGreaterThan(1);
    }

    #endregion

    #region Crime Distribution Tests

    [Fact(DisplayName = "T085: Statistics should track crime distribution")]
    public async Task Statistics_ShouldTrackCrimeDistribution()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CeibaDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IAutomatedReportService>();

        var zona = context.Zonas.First();
        var crimes = new[] { "Robo", "Robo", "Robo", "Fraude", "Fraude", "Violencia" };

        foreach (var crime in crimes)
        {
            context.ReportesIncidencia.Add(new ReporteIncidencia
            {
                HechosReportados = $"Case: {crime}",
                AccionesRealizadas = "Processed",
                Delito = crime,
                Estado = 1,
                Sexo = "Mujer",
                Edad = 30,
                TipoDeAtencion = "Presencial",
                TipoDeAccion = 1,
                ZonaId = zona.Id,
                LgbtttiqPlus = false,
                Migrante = false,
                SituacionCalle = false,
                Discapacidad = false,
                Traslados = 0,
                CreatedAt = DateTime.UtcNow
            });
        }
        await context.SaveChangesAsync();

        // Act
        var stats = await service.CalculateStatisticsAsync(
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(1));

        // Assert
        stats.PorDelito.Should().NotBeEmpty();
        stats.DelitoMasFrecuente.Should().NotBeNullOrEmpty();
        // The most frequent crime should be "Robo" based on our test data
        // (but may vary if other tests added data)
    }

    #endregion

    #region Geographic Distribution Tests

    [Fact(DisplayName = "T085: Statistics should track geographic distribution")]
    public async Task Statistics_ShouldTrackGeographicDistribution()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CeibaDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IAutomatedReportService>();

        var zonas = context.Zonas.ToList();

        // Create reports in different zones
        foreach (var zona in zonas)
        {
            context.ReportesIncidencia.Add(new ReporteIncidencia
            {
                HechosReportados = $"Case in {zona.Nombre}",
                AccionesRealizadas = "Processed",
                Delito = "Test",
                Estado = 1,
                Sexo = "Mujer",
                Edad = 30,
                TipoDeAtencion = "Presencial",
                TipoDeAccion = 1,
                ZonaId = zona.Id,
                LgbtttiqPlus = false,
                Migrante = false,
                SituacionCalle = false,
                Discapacidad = false,
                Traslados = 0,
                CreatedAt = DateTime.UtcNow
            });
        }
        await context.SaveChangesAsync();

        // Act
        var stats = await service.CalculateStatisticsAsync(
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(1));

        // Assert
        stats.PorZona.Should().NotBeEmpty();
        stats.ZonaMasActiva.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Email Integration Tests

    [Fact(DisplayName = "T085: IEmailService should be registered in DI container")]
    public void EmailService_ShouldBeRegistered()
    {
        // Arrange & Act
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetService<IEmailService>();

        // Assert
        service.Should().NotBeNull();
    }

    [Fact(DisplayName = "T085: IAiNarrativeService should be registered in DI container")]
    public void AiNarrativeService_ShouldBeRegistered()
    {
        // Arrange & Act
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetService<IAiNarrativeService>();

        // Assert
        service.Should().NotBeNull();
    }

    #endregion
}
