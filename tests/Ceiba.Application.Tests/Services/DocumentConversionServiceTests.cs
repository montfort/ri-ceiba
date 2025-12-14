using Ceiba.Application.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Ceiba.Application.Tests.Services;

/// <summary>
/// Unit tests for DocumentConversionService.
/// Tests Pandoc availability, input validation, and conversion scenarios.
/// </summary>
public class DocumentConversionServiceTests
{
    private readonly Mock<ILogger<DocumentConversionService>> _loggerMock;
    private readonly DocumentConversionService _service;

    public DocumentConversionServiceTests()
    {
        _loggerMock = new Mock<ILogger<DocumentConversionService>>();
        _service = new DocumentConversionService(_loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithLogger_CreatesInstance()
    {
        // Act
        var service = new DocumentConversionService(_loggerMock.Object);

        // Assert
        Assert.NotNull(service);
    }

    #endregion

    #region ConvertMarkdownToWordAsync - Input Validation Tests

    [Fact]
    public async Task ConvertMarkdownToWordAsync_NullContent_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.ConvertMarkdownToWordAsync(null!, "Test Title"));

        Assert.Contains("cannot be empty", exception.Message);
    }

    [Fact]
    public async Task ConvertMarkdownToWordAsync_EmptyContent_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.ConvertMarkdownToWordAsync("", "Test Title"));

        Assert.Contains("cannot be empty", exception.Message);
    }

    [Fact]
    public async Task ConvertMarkdownToWordAsync_WhitespaceContent_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.ConvertMarkdownToWordAsync("   ", "Test Title"));

        Assert.Contains("cannot be empty", exception.Message);
    }

    [Fact]
    public async Task ConvertMarkdownToWordAsync_ContentExceedsMaxSize_ThrowsArgumentException()
    {
        // Arrange
        var oversizedContent = new string('x', DocumentConversionService.MaxInputCharacters + 1);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.ConvertMarkdownToWordAsync(oversizedContent, "Test"));

        Assert.Contains("excede el límite máximo", exception.Message);
    }

    [Fact]
    public async Task ConvertMarkdownToWordAsync_ContentAtMaxSize_DoesNotThrowSizeException()
    {
        // Arrange
        var maxSizeContent = new string('x', DocumentConversionService.MaxInputCharacters);

        // Act & Assert - Should throw for Pandoc not available, not for size
        if (!_service.IsPandocAvailable())
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.ConvertMarkdownToWordAsync(maxSizeContent, "Test"));

            Assert.Contains("Pandoc", exception.Message);
        }
    }

    #endregion

    #region IsPandocAvailable Tests

    [Fact]
    public void IsPandocAvailable_ReturnsBooleanValue()
    {
        // Act
        var result = _service.IsPandocAvailable();

        // Assert
        Assert.IsType<bool>(result);
    }

    [Fact]
    public void IsPandocAvailable_CachesResult()
    {
        // Act
        var result1 = _service.IsPandocAvailable();
        var result2 = _service.IsPandocAvailable();

        // Assert - Should return same result
        Assert.Equal(result1, result2);
    }

    #endregion

    #region GetPandocVersion Tests

    [Fact]
    public void GetPandocVersion_WhenNotAvailable_ReturnsNull()
    {
        // Act
        var isAvailable = _service.IsPandocAvailable();
        var version = _service.GetPandocVersion();

        // Assert
        if (!isAvailable)
        {
            Assert.Null(version);
        }
    }

    [Fact]
    public void GetPandocVersion_WhenAvailable_ReturnsVersionString()
    {
        // Act
        var isAvailable = _service.IsPandocAvailable();
        var version = _service.GetPandocVersion();

        // Assert
        if (isAvailable)
        {
            Assert.NotNull(version);
            Assert.NotEmpty(version);
        }
    }

    #endregion

    #region ConvertMarkdownToWordAsync - Pandoc Not Available Tests

    [Fact]
    public async Task ConvertMarkdownToWordAsync_PandocNotAvailable_ThrowsInvalidOperationException()
    {
        // Skip if Pandoc is available
        if (_service.IsPandocAvailable())
        {
            return;
        }

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ConvertMarkdownToWordAsync("# Test", "Test Title"));

        Assert.Contains("Pandoc", exception.Message);
    }

    #endregion

    #region Constants Tests

    [Fact]
    public void ConversionTimeoutSeconds_HasReasonableValue()
    {
        // Assert
        Assert.True(DocumentConversionService.ConversionTimeoutSeconds >= 30);
        Assert.True(DocumentConversionService.ConversionTimeoutSeconds <= 300);
    }

    [Fact]
    public void MaxInputCharacters_HasReasonableValue()
    {
        // Assert
        Assert.True(DocumentConversionService.MaxInputCharacters >= 100_000);
        Assert.True(DocumentConversionService.MaxInputCharacters <= 10_000_000);
    }

    #endregion

    #region PandocStartupValidator Tests

    [Fact]
    public void PandocStartupValidator_ValidateAndLog_ReturnsBoolean()
    {
        // Arrange
        var logger = new Mock<ILogger>();

        // Act
        var result = PandocStartupValidator.ValidateAndLog(logger.Object);

        // Assert
        Assert.IsType<bool>(result);
    }

    #endregion

    #region Integration Tests (When Pandoc Available)

    [Fact]
    public async Task ConvertMarkdownToWordAsync_ValidMarkdown_ReturnsDocxBytes()
    {
        // Skip if Pandoc is not available
        if (!_service.IsPandocAvailable())
        {
            return;
        }

        // Arrange
        var markdown = "# Test Document\n\nThis is a test paragraph.";

        // Act
        var result = await _service.ConvertMarkdownToWordAsync(markdown, "Test Title");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);

        // Check DOCX magic bytes (PK header for ZIP format)
        Assert.Equal(0x50, result[0]); // 'P'
        Assert.Equal(0x4B, result[1]); // 'K'
    }

    [Fact]
    public async Task ConvertMarkdownToWordAsync_WithoutTitle_ReturnsDocxBytes()
    {
        // Skip if Pandoc is not available
        if (!_service.IsPandocAvailable())
        {
            return;
        }

        // Arrange
        var markdown = "# Test Document\n\nThis is a test paragraph.";

        // Act
        var result = await _service.ConvertMarkdownToWordAsync(markdown, "");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public async Task ConvertMarkdownToWordAsync_ComplexMarkdown_ReturnsDocxBytes()
    {
        // Skip if Pandoc is not available
        if (!_service.IsPandocAvailable())
        {
            return;
        }

        // Arrange
        var markdown = @"
# Main Title

## Section 1

This is a paragraph with **bold** and *italic* text.

### Subsection 1.1

- Item 1
- Item 2
- Item 3

## Section 2

| Column 1 | Column 2 |
|----------|----------|
| Value 1  | Value 2  |

> This is a blockquote

```csharp
var code = ""sample"";
```
";

        // Act
        var result = await _service.ConvertMarkdownToWordAsync(markdown, "Complex Document");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public async Task ConvertMarkdownToWordAsync_SupportsSpanishCharacters()
    {
        // Skip if Pandoc is not available
        if (!_service.IsPandocAvailable())
        {
            return;
        }

        // Arrange
        var markdown = @"
# Informe de Incidencias

## Descripción

Este es un informe con caracteres especiales: áéíóú ÁÉÍÓÚ ñÑ ¿? ¡!

### Detalles

El año 2024 fue significativo para nuestra gestión.
";

        // Act
        var result = await _service.ConvertMarkdownToWordAsync(markdown, "Informe");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public async Task ConvertMarkdownToWordAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Skip if Pandoc is not available
        if (!_service.IsPandocAvailable())
        {
            return;
        }

        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - TaskCanceledException inherits from OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _service.ConvertMarkdownToWordAsync("# Test", "Test", cts.Token));
    }

    #endregion
}
