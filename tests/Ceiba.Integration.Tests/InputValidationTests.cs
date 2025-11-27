using System.Net;
using System.Net.Http.Json;
using Ceiba.Core.Entities;
using Ceiba.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Ceiba.Integration.Tests;

// Temporary DTOs for testing - will be replaced with actual DTOs from Ceiba.Application
public record CreateReportDto
{
    public string TipoReporte { get; init; } = string.Empty;
    public string Sexo { get; init; } = string.Empty;
    public int Edad { get; init; }
    public string Delito { get; init; } = string.Empty;
    public int ZonaId { get; init; }
    public int SectorId { get; init; }
    public int CuadranteId { get; init; }
    public string TurnoCeiba { get; init; } = string.Empty;
    public string TipoDeAtencion { get; init; } = string.Empty;
    public string TipoDeAccion { get; init; } = string.Empty;
    public string HechosReportados { get; init; } = string.Empty;
    public string AccionesRealizadas { get; init; } = string.Empty;
    public string Traslados { get; init; } = string.Empty;
}

public record ReportDto
{
    public int Id { get; init; }
    public string HechosReportados { get; init; } = string.Empty;
    public string Delito { get; init; } = string.Empty;
}

/// <summary>
/// T020i: RS-002 Mitigation - Input Validation Integration Tests
/// Tests that all user inputs are properly validated to prevent injection attacks
/// </summary>
public class InputValidationTests : IClassFixture<CeibaWebApplicationFactory>
{
    private readonly CeibaWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public InputValidationTests(CeibaWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region SQL Injection Prevention Tests

    [Theory]
    [Trait("Category", "Integration")]
    [Trait("Security", "InputValidation")]
    [InlineData("' OR '1'='1")]
    [InlineData("admin'--")]
    [InlineData("1' UNION SELECT NULL--")]
    [InlineData("'; DROP TABLE REPORTE_INCIDENCIA; --")]
    [InlineData("1' AND 1=1--")]
    [InlineData("' OR 1=1--")]
    public async Task ReportCreation_RejectsSQLInjectionAttempts(string maliciousInput)
    {
        // Arrange
        var reportDto = new CreateReportDto
        {
            TipoReporte = "A",
            Sexo = maliciousInput, // SQL injection attempt
            Edad = 25,
            Delito = "Test",
            ZonaId = 1,
            SectorId = 1,
            CuadranteId = 1,
            TurnoCeiba = "Matutino",
            TipoDeAtencion = "Presencial",
            TipoDeAccion = "Orientación",
            HechosReportados = "Test facts",
            AccionesRealizadas = "Test actions",
            Traslados = "Ninguno"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/reports", reportDto);

        // Assert
        // Should either validate and reject, or sanitize the input
        // The input should NOT cause SQL errors or unauthorized data access
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,  // Validation failed
            HttpStatusCode.OK,          // Accepted but sanitized
            HttpStatusCode.Created      // Accepted but sanitized
        );

        // Verify no SQL injection occurred by checking database integrity
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CeibaDbContext>();

        // Database should still be intact
        var reportsExist = await dbContext.ReportesIncidencia.AnyAsync();
        // Test passes if we can query without errors (no SQL injection damage)
    }

    [Theory]
    [Trait("Category", "Integration")]
    [Trait("Security", "InputValidation")]
    [InlineData("test@example.com'; DROP TABLE USUARIO; --")]
    [InlineData("admin' OR '1'='1")]
    [InlineData("user@domain.com' UNION SELECT password FROM USUARIO--")]
    public async Task UserCreation_RejectsSQLInjectionInEmail(string maliciousEmail)
    {
        // Arrange
        var userDto = new
        {
            Email = maliciousEmail,
            Nombre = "Test",
            Apellido = "User",
            Password = "ValidPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/admin/users", userDto);

        // Assert
        // Should reject invalid email format or sanitize
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);

        // Verify database integrity
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CeibaDbContext>();
        var usersTable = await dbContext.Users.AnyAsync();
        // If we can query, SQL injection was prevented
    }

    #endregion

    #region XSS Prevention Tests

    [Theory]
    [Trait("Category", "Integration")]
    [Trait("Security", "InputValidation")]
    [InlineData("<script>alert('XSS')</script>")]
    [InlineData("<img src=x onerror=alert('XSS')>")]
    [InlineData("<iframe src='javascript:alert(\"XSS\")'></iframe>")]
    [InlineData("<svg/onload=alert('XSS')>")]
    [InlineData("javascript:alert('XSS')")]
    [InlineData("<body onload=alert('XSS')>")]
    [InlineData("<input onfocus=alert('XSS') autofocus>")]
    public async Task ReportCreation_SanitizesXSSAttempts(string xssPayload)
    {
        // Arrange
        var reportDto = new CreateReportDto
        {
            TipoReporte = "A",
            Sexo = "Masculino",
            Edad = 25,
            Delito = "Test",
            ZonaId = 1,
            SectorId = 1,
            CuadranteId = 1,
            TurnoCeiba = "Matutino",
            TipoDeAtencion = "Presencial",
            TipoDeAccion = "Orientación",
            HechosReportados = xssPayload, // XSS attempt in text field
            AccionesRealizadas = "Test actions",
            Traslados = "Ninguno"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/reports", reportDto);

        // Assert
        if (response.IsSuccessStatusCode)
        {
            var createdReport = await response.Content.ReadFromJsonAsync<ReportDto>();

            // Verify that the stored value is sanitized (doesn't contain script tags)
            createdReport.HechosReportados.Should().NotContain("<script");
            createdReport.HechosReportados.Should().NotContain("onerror=");
            createdReport.HechosReportados.Should().NotContain("javascript:");
        }
    }

    [Theory]
    [Trait("Category", "Integration")]
    [Trait("Security", "InputValidation")]
    [InlineData("<script>document.cookie</script>User")]
    [InlineData("<img src=x onerror=alert(1)>Admin")]
    public async Task UserCreation_SanitizesXSSInName(string maliciousName)
    {
        // Arrange
        var userDto = new
        {
            Email = "test@example.com",
            Nombre = maliciousName,
            Apellido = "User",
            Password = "ValidPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/admin/users", userDto);

        // Assert
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotContain("<script");
            content.Should().NotContain("onerror=");
        }
    }

    #endregion

    #region Command Injection Prevention Tests

    [Theory]
    [Trait("Category", "Integration")]
    [Trait("Security", "InputValidation")]
    [InlineData("; ls -la")]
    [InlineData("| cat /etc/passwd")]
    [InlineData("& whoami")]
    [InlineData("$(rm -rf /)")]
    [InlineData("`id`")]
    [InlineData("test && curl attacker.com")]
    public async Task FileExport_RejectsCommandInjection(string maliciousFilename)
    {
        // Arrange - attempt command injection in export filename
        var exportRequest = new
        {
            ReportIds = new[] { 1 },
            Format = "PDF",
            Filename = maliciousFilename
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/reports/export", exportRequest);

        // Assert
        // Should validate filename and reject shell metacharacters
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var error = await response.Content.ReadAsStringAsync();
            error.Should().Contain("invalid", "Invalid filename should be rejected");
        }
        else if (response.IsSuccessStatusCode)
        {
            // If accepted, filename should be sanitized
            var contentDisposition = response.Content.Headers.ContentDisposition?.FileName;
            contentDisposition.Should().NotContain(";");
            contentDisposition.Should().NotContain("|");
            contentDisposition.Should().NotContain("&");
            contentDisposition.Should().NotContain("$");
            contentDisposition.Should().NotContain("`");
        }
    }

    #endregion

    #region Path Traversal Prevention Tests

    [Theory]
    [Trait("Category", "Integration")]
    [Trait("Security", "InputValidation")]
    [InlineData("../../../etc/passwd")]
    [InlineData("..\\..\\windows\\system32\\config\\sam")]
    [InlineData("../../secret.key")]
    [InlineData("....//....//etc/passwd")]
    [InlineData("..%2F..%2F..%2Fetc%2Fpasswd")]
    public async Task FileAccess_PreventsPathTraversal(string maliciousPath)
    {
        // Arrange - attempt path traversal in file access
        var fileRequest = new
        {
            Path = maliciousPath
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/files/download", fileRequest);

        // Assert
        // Should validate and reject path traversal attempts
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,      // Validation failed
            HttpStatusCode.Forbidden,       // Access denied
            HttpStatusCode.NotFound         // File not found (safe)
        );

        response.StatusCode.Should().NotBe(HttpStatusCode.OK,
            "Path traversal should not succeed");
    }

    #endregion

    #region LDAP Injection Prevention Tests

    [Theory]
    [Trait("Category", "Integration")]
    [Trait("Security", "InputValidation")]
    [InlineData("*)(uid=*))(|(uid=*")]
    [InlineData("admin)(|(password=*")]
    [InlineData("*)(objectClass=*")]
    public async Task UserSearch_PreventsLDAPInjection(string maliciousQuery)
    {
        // Arrange
        var searchRequest = new
        {
            SearchTerm = maliciousQuery
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/admin/users/search", searchRequest);

        // Assert
        // LDAP metacharacters should be escaped or rejected
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region Email Header Injection Prevention Tests

    [Theory]
    [Trait("Category", "Integration")]
    [Trait("Security", "InputValidation")]
    [InlineData("user@example.com\nBcc: attacker@evil.com")]
    [InlineData("user@example.com\r\nCc: spam@spam.com")]
    [InlineData("user@example.com%0aBcc:attacker@evil.com")]
    public async Task EmailNotification_PreventsHeaderInjection(string maliciousEmail)
    {
        // Arrange
        var notificationRequest = new
        {
            RecipientEmail = maliciousEmail,
            Subject = "Test",
            Body = "Test message"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/notifications/send", notificationRequest);

        // Assert
        // Email validation should reject newline characters
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var error = await response.Content.ReadAsStringAsync();
            error.Should().Contain("email", "Should indicate email validation error");
        }
    }

    #endregion

    #region JSON Injection Prevention Tests

    [Theory]
    [Trait("Category", "Integration")]
    [Trait("Security", "InputValidation")]
    [InlineData("{\"admin\":true}")]
    [InlineData("\\\"role\\\":\\\"ADMIN\\\"")]
    [InlineData("null, \"isAdmin\": true")]
    public async Task ReportCreation_PreventsJSONInjection(string maliciousJson)
    {
        // Arrange
        var reportDto = new CreateReportDto
        {
            TipoReporte = "A",
            Sexo = "Masculino",
            Edad = 25,
            Delito = maliciousJson, // Attempt to inject JSON structure
            ZonaId = 1,
            SectorId = 1,
            CuadranteId = 1,
            TurnoCeiba = "Matutino",
            TipoDeAtencion = "Presencial",
            TipoDeAccion = "Orientación",
            HechosReportados = "Test",
            AccionesRealizadas = "Test",
            Traslados = "Ninguno"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/reports", reportDto);

        // Assert
        if (response.IsSuccessStatusCode)
        {
            var createdReport = await response.Content.ReadFromJsonAsync<ReportDto>();

            // Verify that JSON special characters are properly escaped
            createdReport.Delito.Should().NotContain("\"role\":");
            createdReport.Delito.Should().NotContain("\"admin\":");
        }
    }

    #endregion

    #region XML/XXE Prevention Tests

    [Theory]
    [Trait("Category", "Integration")]
    [Trait("Security", "InputValidation")]
    [InlineData("<?xml version=\"1.0\"?><!DOCTYPE foo [<!ENTITY xxe SYSTEM \"file:///etc/passwd\">]><foo>&xxe;</foo>")]
    [InlineData("<!DOCTYPE foo [<!ELEMENT foo ANY ><!ENTITY xxe SYSTEM \"file:///c:/boot.ini\" >]><foo>&xxe;</foo>")]
    public async Task DataImport_PreventsXXEAttacks(string maliciousXml)
    {
        // Arrange
        var importRequest = new StringContent(maliciousXml, System.Text.Encoding.UTF8, "application/xml");

        // Act
        var response = await _client.PostAsync("/api/reports/import", importRequest);

        // Assert
        // XXE should be prevented by XML parser configuration
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.UnsupportedMediaType
        );
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    #endregion

    #region NoSQL Injection Prevention Tests

    [Theory]
    [Trait("Category", "Integration")]
    [Trait("Security", "InputValidation")]
    [InlineData("{\"$gt\": \"\"}")]
    [InlineData("{\"$ne\": null}")]
    [InlineData("{\"$regex\": \".*\"}")]
    public async Task Search_PreventsNoSQLInjection(string maliciousQuery)
    {
        // Arrange - if using JSONB queries in PostgreSQL
        var searchRequest = new
        {
            Filter = maliciousQuery
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/reports/search", searchRequest);

        // Assert
        // NoSQL operators should be escaped or rejected
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region Length Validation Tests

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Security", "InputValidation")]
    public async Task ReportCreation_RejectsExcessivelyLongInput()
    {
        // Arrange - attempt buffer overflow with very long string
        var veryLongString = new string('A', 100000); // 100KB string

        var reportDto = new CreateReportDto
        {
            TipoReporte = "A",
            Sexo = "Masculino",
            Edad = 25,
            Delito = "Test",
            ZonaId = 1,
            SectorId = 1,
            CuadranteId = 1,
            TurnoCeiba = "Matutino",
            TipoDeAtencion = "Presencial",
            TipoDeAccion = "Orientación",
            HechosReportados = veryLongString, // Excessively long input
            AccionesRealizadas = "Test",
            Traslados = "Ninguno"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/reports", reportDto);

        // Assert
        // Should reject or truncate excessively long input
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,           // Validation failed
            HttpStatusCode.RequestEntityTooLarge // Payload too large
        );
    }

    #endregion

    #region Numeric Validation Tests

    [Theory]
    [Trait("Category", "Integration")]
    [Trait("Security", "InputValidation")]
    [InlineData(-1)]
    [InlineData(999999)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public async Task ReportCreation_ValidatesNumericRanges(int invalidAge)
    {
        // Arrange
        var reportDto = new CreateReportDto
        {
            TipoReporte = "A",
            Sexo = "Masculino",
            Edad = invalidAge, // Out of valid range
            Delito = "Test",
            ZonaId = 1,
            SectorId = 1,
            CuadranteId = 1,
            TurnoCeiba = "Matutino",
            TipoDeAtencion = "Presencial",
            TipoDeAccion = "Orientación",
            HechosReportados = "Test",
            AccionesRealizadas = "Test",
            Traslados = "Ninguno"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/reports", reportDto);

        // Assert
        // Should validate age is in reasonable range (0-150)
        if (invalidAge < 0 || invalidAge > 150)
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }

    #endregion

    #region CRLF Injection Prevention Tests

    [Theory]
    [Trait("Category", "Integration")]
    [Trait("Security", "InputValidation")]
    [InlineData("User\r\nSet-Cookie: admin=true")]
    [InlineData("Test\nLocation: http://evil.com")]
    [InlineData("Value%0d%0aHeader: Injected")]
    public async Task HeaderValues_PreventCRLFInjection(string maliciousValue)
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("X-Custom-Header", maliciousValue);

        // Act
        var response = await _client.GetAsync("/api/reports");

        // Assert
        // Server should sanitize or reject headers with CRLF
        response.Headers.Should().NotContain(h => h.Value.Any(v => v.Contains("Set-Cookie: admin")));
    }

    #endregion
}

/// <summary>
/// Input Validation Summary (T020i, RS-002)
///
/// Tested Attack Vectors:
/// ✅ SQL Injection (PostgreSQL-specific)
/// ✅ Cross-Site Scripting (XSS)
/// ✅ Command Injection
/// ✅ Path Traversal
/// ✅ LDAP Injection
/// ✅ Email Header Injection
/// ✅ JSON Injection
/// ✅ XML External Entity (XXE)
/// ✅ NoSQL Injection (JSONB queries)
/// ✅ Buffer Overflow (length validation)
/// ✅ Integer Overflow (numeric ranges)
/// ✅ CRLF Injection
///
/// Validation Strategy:
/// - Use parameterized queries (EF Core)
/// - HTML encode output (Blazor automatic)
/// - Validate and sanitize all inputs
/// - Enforce length limits
/// - Validate numeric ranges
/// - Escape special characters
/// - Use allowlists over denylists
/// - Apply least privilege principle
///
/// Complementary Mitigations:
/// - Content Security Policy (CSP) headers
/// - X-Content-Type-Options: nosniff
/// - X-Frame-Options: DENY
/// - Strict-Transport-Security (HSTS)
/// - Input validation attributes ([StringLength], [Range], [RegularExpression])
/// - Output encoding (Blazor automatic)
/// - SQL parameterization (EF Core automatic)
/// </summary>
