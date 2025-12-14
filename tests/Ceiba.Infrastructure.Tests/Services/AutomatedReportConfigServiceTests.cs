using System.Security.Claims;
using Ceiba.Core.Entities;
using Ceiba.Infrastructure.Data;
using Ceiba.Infrastructure.Services;
using Ceiba.Shared.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Ceiba.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for AutomatedReportConfigService.
/// Tests configuration management for automated report generation.
/// </summary>
public class AutomatedReportConfigServiceTests : IDisposable
{
    private readonly CeibaDbContext _context;
    private readonly Mock<ILogger<AutomatedReportConfigService>> _loggerMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Guid _testUserId = Guid.NewGuid();

    public AutomatedReportConfigServiceTests()
    {
        var options = new DbContextOptionsBuilder<CeibaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CeibaDbContext(options);
        _loggerMock = new Mock<ILogger<AutomatedReportConfigService>>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

        SetupAuthenticatedUser(_testUserId);
    }

    private void SetupAuthenticatedUser(Guid userId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
    }

    private void SetupUnauthenticatedUser()
    {
        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
    }

    private AutomatedReportConfigService CreateService()
    {
        return new AutomatedReportConfigService(
            _context,
            _loggerMock.Object,
            _httpContextAccessorMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region GetConfigurationAsync Tests

    [Fact]
    public async Task GetConfigurationAsync_NoConfiguration_ReturnsNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetConfigurationAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetConfigurationAsync_ConfigurationExists_ReturnsDto()
    {
        // Arrange
        var config = new ConfiguracionReportesAutomatizados
        {
            Habilitado = true,
            HoraGeneracion = new TimeSpan(6, 0, 0),
            Destinatarios = "test@test.com;admin@test.com",
            RutaSalida = "./reports",
            UsuarioId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };
        _context.ConfiguracionReportesAutomatizados.Add(config);
        await _context.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.GetConfigurationAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Habilitado);
        Assert.Equal(new TimeSpan(6, 0, 0), result.HoraGeneracion);
        Assert.Equal("./reports", result.RutaSalida);
    }

    [Fact]
    public async Task GetConfigurationAsync_MultipleConfigurations_ReturnsMostRecent()
    {
        // Arrange
        var oldConfig = new ConfiguracionReportesAutomatizados
        {
            Habilitado = false,
            HoraGeneracion = new TimeSpan(5, 0, 0),
            RutaSalida = "./old-reports",
            UsuarioId = _testUserId,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        var newConfig = new ConfiguracionReportesAutomatizados
        {
            Habilitado = true,
            HoraGeneracion = new TimeSpan(7, 0, 0),
            RutaSalida = "./new-reports",
            UsuarioId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };
        _context.ConfiguracionReportesAutomatizados.AddRange(oldConfig, newConfig);
        await _context.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.GetConfigurationAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Habilitado);
        Assert.Equal(new TimeSpan(7, 0, 0), result.HoraGeneracion);
        Assert.Equal("./new-reports", result.RutaSalida);
    }

    #endregion

    #region UpdateConfigurationAsync Tests

    [Fact]
    public async Task UpdateConfigurationAsync_UnauthenticatedUser_ThrowsUnauthorized()
    {
        // Arrange
        SetupUnauthenticatedUser();
        var service = CreateService();
        var dto = new AutomatedReportConfigUpdateDto
        {
            Habilitado = true,
            HoraGeneracion = TimeSpan.FromHours(6),
            Destinatarios = Array.Empty<string>()
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.UpdateConfigurationAsync(dto));
    }

    [Fact]
    public async Task UpdateConfigurationAsync_NewConfiguration_CreatesConfig()
    {
        // Arrange
        var service = CreateService();
        var dto = new AutomatedReportConfigUpdateDto
        {
            Habilitado = true,
            HoraGeneracion = new TimeSpan(8, 30, 0),
            Destinatarios = new[] { "user1@test.com", "user2@test.com" },
            RutaSalida = "./generated"
        };

        // Act
        var result = await service.UpdateConfigurationAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Habilitado);
        Assert.Equal(new TimeSpan(8, 30, 0), result.HoraGeneracion);
        Assert.Equal("./generated", result.RutaSalida);
        Assert.Contains("user1@test.com", result.Destinatarios);
        Assert.Contains("user2@test.com", result.Destinatarios);
    }

    [Fact]
    public async Task UpdateConfigurationAsync_ExistingConfiguration_UpdatesConfig()
    {
        // Arrange
        var existingConfig = new ConfiguracionReportesAutomatizados
        {
            Habilitado = false,
            HoraGeneracion = new TimeSpan(5, 0, 0),
            Destinatarios = "old@test.com",
            RutaSalida = "./old",
            UsuarioId = _testUserId,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.ConfiguracionReportesAutomatizados.Add(existingConfig);
        await _context.SaveChangesAsync();

        var service = CreateService();
        var dto = new AutomatedReportConfigUpdateDto
        {
            Habilitado = true,
            HoraGeneracion = new TimeSpan(10, 0, 0),
            Destinatarios = new[] { "new@test.com" },
            RutaSalida = "./new"
        };

        // Act
        var result = await service.UpdateConfigurationAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Habilitado);
        Assert.Equal(new TimeSpan(10, 0, 0), result.HoraGeneracion);
        Assert.Equal("./new", result.RutaSalida);
    }

    [Fact]
    public async Task UpdateConfigurationAsync_SetsUpdatedAt()
    {
        // Arrange
        var service = CreateService();
        var dto = new AutomatedReportConfigUpdateDto
        {
            Habilitado = true,
            HoraGeneracion = TimeSpan.FromHours(6),
            Destinatarios = Array.Empty<string>()
        };

        // Act
        var beforeUpdate = DateTime.UtcNow;
        var result = await service.UpdateConfigurationAsync(dto);
        var afterUpdate = DateTime.UtcNow;

        // Assert
        Assert.NotNull(result.UpdatedAt);
        Assert.True(result.UpdatedAt >= beforeUpdate && result.UpdatedAt <= afterUpdate);
    }

    [Fact]
    public async Task UpdateConfigurationAsync_EmptyRecipients_SavesSuccessfully()
    {
        // Arrange
        var service = CreateService();
        var dto = new AutomatedReportConfigUpdateDto
        {
            Habilitado = false,
            HoraGeneracion = TimeSpan.FromHours(6),
            Destinatarios = Array.Empty<string>()
        };

        // Act
        var result = await service.UpdateConfigurationAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Destinatarios);
    }

    [Fact]
    public async Task UpdateConfigurationAsync_MultipleRecipients_SavesAll()
    {
        // Arrange
        var service = CreateService();
        var recipients = new[] { "a@test.com", "b@test.com", "c@test.com" };
        var dto = new AutomatedReportConfigUpdateDto
        {
            Habilitado = true,
            HoraGeneracion = TimeSpan.FromHours(6),
            Destinatarios = recipients
        };

        // Act
        var result = await service.UpdateConfigurationAsync(dto);

        // Assert
        Assert.Equal(3, result.Destinatarios.Length);
    }

    #endregion

    #region EnsureConfigurationExistsAsync Tests

    [Fact]
    public async Task EnsureConfigurationExistsAsync_NoConfiguration_CreatesDefault()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.EnsureConfigurationExistsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Habilitado);
        Assert.Equal(new TimeSpan(6, 0, 0), result.HoraGeneracion);
        Assert.Equal("./generated-reports", result.RutaSalida);
    }

    [Fact]
    public async Task EnsureConfigurationExistsAsync_ConfigurationExists_ReturnsExisting()
    {
        // Arrange
        var existingConfig = new ConfiguracionReportesAutomatizados
        {
            Habilitado = true,
            HoraGeneracion = new TimeSpan(9, 0, 0),
            RutaSalida = "./custom-path",
            UsuarioId = _testUserId,
            CreatedAt = DateTime.UtcNow
        };
        _context.ConfiguracionReportesAutomatizados.Add(existingConfig);
        await _context.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.EnsureConfigurationExistsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Habilitado);
        Assert.Equal(new TimeSpan(9, 0, 0), result.HoraGeneracion);
        Assert.Equal("./custom-path", result.RutaSalida);
    }

    [Fact]
    public async Task EnsureConfigurationExistsAsync_CalledMultipleTimes_DoesNotDuplicate()
    {
        // Arrange
        var service = CreateService();

        // Act
        await service.EnsureConfigurationExistsAsync();
        await service.EnsureConfigurationExistsAsync();
        await service.EnsureConfigurationExistsAsync();

        // Assert
        var count = await _context.ConfiguracionReportesAutomatizados.CountAsync();
        Assert.Equal(1, count);
    }

    #endregion

    #region DTO Mapping Tests

    [Fact]
    public async Task GetConfigurationAsync_MapsAllPropertiesToDto()
    {
        // Arrange
        var config = new ConfiguracionReportesAutomatizados
        {
            Habilitado = true,
            HoraGeneracion = new TimeSpan(14, 30, 0),
            Destinatarios = "user1@test.com;user2@test.com",
            RutaSalida = "./output",
            UsuarioId = _testUserId,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };
        _context.ConfiguracionReportesAutomatizados.Add(config);
        await _context.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.GetConfigurationAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(config.Id, result.Id);
        Assert.True(result.Habilitado);
        Assert.Equal(new TimeSpan(14, 30, 0), result.HoraGeneracion);
        Assert.Equal("./output", result.RutaSalida);
        Assert.True(result.CreatedAt != default);
        Assert.True(result.UpdatedAt != default);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task UpdateConfigurationAsync_MidnightGenerationTime_HandledCorrectly()
    {
        // Arrange
        var service = CreateService();
        var dto = new AutomatedReportConfigUpdateDto
        {
            Habilitado = true,
            HoraGeneracion = TimeSpan.Zero, // Midnight
            Destinatarios = Array.Empty<string>()
        };

        // Act
        var result = await service.UpdateConfigurationAsync(dto);

        // Assert
        Assert.Equal(TimeSpan.Zero, result.HoraGeneracion);
    }

    [Fact]
    public async Task UpdateConfigurationAsync_EndOfDayGenerationTime_HandledCorrectly()
    {
        // Arrange
        var service = CreateService();
        var dto = new AutomatedReportConfigUpdateDto
        {
            Habilitado = true,
            HoraGeneracion = new TimeSpan(23, 59, 59),
            Destinatarios = Array.Empty<string>()
        };

        // Act
        var result = await service.UpdateConfigurationAsync(dto);

        // Assert
        Assert.Equal(new TimeSpan(23, 59, 59), result.HoraGeneracion);
    }

    #endregion
}
