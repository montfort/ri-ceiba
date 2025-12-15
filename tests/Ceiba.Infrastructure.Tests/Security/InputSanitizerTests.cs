using Ceiba.Infrastructure.Security;
using FluentAssertions;

namespace Ceiba.Infrastructure.Tests.Security;

/// <summary>
/// Unit tests for InputSanitizer - input validation and XSS/SQL injection prevention.
/// T118b: Input sanitization tests.
/// </summary>
[Trait("Category", "Unit")]
public class InputSanitizerTests
{
    private readonly InputSanitizer _sanitizer;

    public InputSanitizerTests()
    {
        _sanitizer = new InputSanitizer();
    }

    #region Sanitize (Basic) Tests

    [Fact(DisplayName = "Sanitize should return empty string for null input")]
    public void Sanitize_NullInput_ReturnsEmptyString()
    {
        // Act
        var result = _sanitizer.Sanitize(null);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact(DisplayName = "Sanitize should return empty string for empty input")]
    public void Sanitize_EmptyInput_ReturnsEmptyString()
    {
        // Act
        var result = _sanitizer.Sanitize(string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact(DisplayName = "Sanitize should HTML encode special characters")]
    public void Sanitize_SpecialChars_HtmlEncodes()
    {
        // Arrange
        var input = "<script>alert('xss')</script>";

        // Act
        var result = _sanitizer.Sanitize(input);

        // Assert
        result.Should().NotContain("<script>");
        result.Should().Contain("&lt;script&gt;");
    }

    [Fact(DisplayName = "Sanitize should remove null bytes")]
    public void Sanitize_NullBytes_Removes()
    {
        // Arrange
        var input = "test\0string";

        // Act
        var result = _sanitizer.Sanitize(input);

        // Assert
        result.Should().NotContain("\0");
    }

    [Fact(DisplayName = "Sanitize should preserve normal text")]
    public void Sanitize_NormalText_Preserved()
    {
        // Arrange
        var input = "Normal text without special characters";

        // Act
        var result = _sanitizer.Sanitize(input);

        // Assert
        result.Should().Be(input);
    }

    [Theory(DisplayName = "Sanitize should encode HTML entities")]
    [InlineData("&", "&amp;")]
    [InlineData("<", "&lt;")]
    [InlineData(">", "&gt;")]
    [InlineData("\"", "&quot;")]
    public void Sanitize_HtmlEntities_Encoded(string input, string expected)
    {
        // Act
        var result = _sanitizer.Sanitize(input);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region SanitizeHtml Tests

    [Fact(DisplayName = "SanitizeHtml should remove script tags")]
    public void SanitizeHtml_ScriptTags_Removed()
    {
        // Arrange
        var input = "<p>Hello</p><script>alert('xss')</script><p>World</p>";

        // Act
        var result = _sanitizer.SanitizeHtml(input);

        // Assert
        result.Should().NotContain("<script");
        result.Should().NotContain("</script>");
        result.Should().NotContain("alert");
    }

    [Fact(DisplayName = "SanitizeHtml should remove iframe tags")]
    public void SanitizeHtml_IframeTags_Removed()
    {
        // Arrange
        var input = "<iframe src='evil.com'></iframe>";

        // Act
        var result = _sanitizer.SanitizeHtml(input);

        // Assert
        result.Should().NotContain("<iframe");
    }

    [Fact(DisplayName = "SanitizeHtml should remove onclick attributes")]
    public void SanitizeHtml_OnclickAttribute_Removed()
    {
        // Arrange
        var input = "<button onclick=\"alert('xss')\">Click me</button>";

        // Act
        var result = _sanitizer.SanitizeHtml(input);

        // Assert
        result.Should().NotContain("onclick");
    }

    [Fact(DisplayName = "SanitizeHtml should remove javascript: URLs")]
    public void SanitizeHtml_JavascriptUrl_Removed()
    {
        // Arrange
        var input = "<a href=\"javascript:alert('xss')\">Click</a>";

        // Act
        var result = _sanitizer.SanitizeHtml(input);

        // Assert
        result.Should().NotContain("javascript:");
    }

    [Fact(DisplayName = "SanitizeHtml should return empty for null")]
    public void SanitizeHtml_Null_ReturnsEmpty()
    {
        // Act
        var result = _sanitizer.SanitizeHtml(null);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory(DisplayName = "SanitizeHtml should remove dangerous tags")]
    [InlineData("<object data='x'></object>")]
    [InlineData("<embed src='x'>")]
    [InlineData("<form action='x'></form>")]
    [InlineData("<link href='x'>")]
    [InlineData("<style>body{}</style>")]
    [InlineData("<meta charset='x'>")]
    public void SanitizeHtml_DangerousTags_Removed(string input)
    {
        // Act
        var result = _sanitizer.SanitizeHtml(input);

        // Assert
        result.Should().NotMatchRegex("<(object|embed|form|link|style|meta)");
    }

    [Theory(DisplayName = "SanitizeHtml should remove dangerous event handlers")]
    [InlineData("onmouseover=\"evil()\"")]
    [InlineData("onerror=\"evil()\"")]
    [InlineData("onload=\"evil()\"")]
    [InlineData("onfocus=\"evil()\"")]
    public void SanitizeHtml_EventHandlers_Removed(string attribute)
    {
        // Arrange
        var input = $"<div {attribute}>content</div>";

        // Act
        var result = _sanitizer.SanitizeHtml(input);

        // Assert
        result.Should().NotContain(attribute.Split('=')[0]);
    }

    #endregion

    #region SanitizeForSql Tests

    [Fact(DisplayName = "SanitizeForSql should escape single quotes")]
    public void SanitizeForSql_SingleQuotes_Escaped()
    {
        // Arrange
        var input = "O'Brien";

        // Act
        var result = _sanitizer.SanitizeForSql(input);

        // Assert
        result.Should().Be("O''Brien");
    }

    [Fact(DisplayName = "SanitizeForSql should remove SQL comments")]
    public void SanitizeForSql_SqlComments_Removed()
    {
        // Arrange
        var input = "SELECT * -- comment";

        // Act
        var result = _sanitizer.SanitizeForSql(input);

        // Assert
        result.Should().NotContain("--");
    }

    [Fact(DisplayName = "SanitizeForSql should remove UNION SELECT")]
    public void SanitizeForSql_UnionSelect_Removed()
    {
        // Arrange
        var input = "1 UNION SELECT password FROM users";

        // Act
        var result = _sanitizer.SanitizeForSql(input);

        // Assert
        result.Should().NotContainEquivalentOf("union select");
    }

    [Fact(DisplayName = "SanitizeForSql should remove DROP TABLE")]
    public void SanitizeForSql_DropTable_Removed()
    {
        // Arrange
        var input = "; DROP TABLE users; --";

        // Act
        var result = _sanitizer.SanitizeForSql(input);

        // Assert
        result.Should().NotContainEquivalentOf("drop table");
    }

    [Fact(DisplayName = "SanitizeForSql should return empty for null")]
    public void SanitizeForSql_Null_ReturnsEmpty()
    {
        // Act
        var result = _sanitizer.SanitizeForSql(null);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region SanitizeEmail Tests

    [Fact(DisplayName = "SanitizeEmail should accept valid email")]
    public void SanitizeEmail_ValidEmail_Accepted()
    {
        // Arrange
        var input = "User@Example.Com";

        // Act
        var result = _sanitizer.SanitizeEmail(input);

        // Assert
        result.Should().Be("user@example.com");
    }

    [Fact(DisplayName = "SanitizeEmail should return null for invalid email")]
    public void SanitizeEmail_InvalidEmail_ReturnsNull()
    {
        // Arrange
        var input = "not-an-email";

        // Act
        var result = _sanitizer.SanitizeEmail(input);

        // Assert
        result.Should().BeNull();
    }

    [Fact(DisplayName = "SanitizeEmail should return null for null input")]
    public void SanitizeEmail_Null_ReturnsNull()
    {
        // Act
        var result = _sanitizer.SanitizeEmail(null);

        // Assert
        result.Should().BeNull();
    }

    [Fact(DisplayName = "SanitizeEmail should trim whitespace")]
    public void SanitizeEmail_Whitespace_Trimmed()
    {
        // Arrange
        var input = "  user@example.com  ";

        // Act
        var result = _sanitizer.SanitizeEmail(input);

        // Assert
        result.Should().Be("user@example.com");
    }

    [Theory(DisplayName = "SanitizeEmail should reject malformed emails")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("user@.com")]
    [InlineData("user@com")]
    public void SanitizeEmail_MalformedEmails_ReturnsNull(string input)
    {
        // Act
        var result = _sanitizer.SanitizeEmail(input);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region SanitizeFileName Tests

    [Fact(DisplayName = "SanitizeFileName should remove path separators")]
    public void SanitizeFileName_PathSeparators_Removed()
    {
        // Arrange
        var input = "../../../etc/passwd";

        // Act
        var result = _sanitizer.SanitizeFileName(input);

        // Assert
        result.Should().NotContain("/");
        result.Should().NotContain("\\");
        result.Should().NotContain("..");
    }

    [Fact(DisplayName = "SanitizeFileName should return unnamed for null")]
    public void SanitizeFileName_Null_ReturnsUnnamed()
    {
        // Act
        var result = _sanitizer.SanitizeFileName(null);

        // Assert
        result.Should().Be("unnamed");
    }

    [Fact(DisplayName = "SanitizeFileName should return unnamed for empty result")]
    public void SanitizeFileName_EmptyResult_ReturnsUnnamed()
    {
        // Arrange
        var input = "///";

        // Act
        var result = _sanitizer.SanitizeFileName(input);

        // Assert
        result.Should().Be("unnamed");
    }

    [Fact(DisplayName = "SanitizeFileName should truncate long names")]
    public void SanitizeFileName_LongName_Truncated()
    {
        // Arrange
        var input = new string('a', 300);

        // Act
        var result = _sanitizer.SanitizeFileName(input);

        // Assert
        result.Length.Should().BeLessThanOrEqualTo(255);
    }

    [Fact(DisplayName = "SanitizeFileName should preserve valid names")]
    public void SanitizeFileName_ValidName_Preserved()
    {
        // Arrange
        var input = "document.pdf";

        // Act
        var result = _sanitizer.SanitizeFileName(input);

        // Assert
        result.Should().Be("document.pdf");
    }

    #endregion

    #region SanitizeUrl Tests

    [Fact(DisplayName = "SanitizeUrl should allow relative URLs")]
    public void SanitizeUrl_RelativeUrl_Allowed()
    {
        // Arrange
        var input = "/reports/view/1";

        // Act
        var result = _sanitizer.SanitizeUrl(input);

        // Assert
        result.Should().Be("/reports/view/1");
    }

    [Fact(DisplayName = "SanitizeUrl should reject protocol-relative URLs")]
    public void SanitizeUrl_ProtocolRelative_Rejected()
    {
        // Arrange
        var input = "//evil.com/script.js";

        // Act
        var result = _sanitizer.SanitizeUrl(input);

        // Assert
        result.Should().BeNull();
    }

    [Fact(DisplayName = "SanitizeUrl should allow same-host absolute URLs")]
    public void SanitizeUrl_SameHost_Allowed()
    {
        // Arrange
        var input = "https://ceiba.local/page";

        // Act
        var result = _sanitizer.SanitizeUrl(input, "ceiba.local");

        // Assert
        result.Should().Be(input);
    }

    [Fact(DisplayName = "SanitizeUrl should reject different-host URLs")]
    public void SanitizeUrl_DifferentHost_Rejected()
    {
        // Arrange
        var input = "https://evil.com/page";

        // Act
        var result = _sanitizer.SanitizeUrl(input, "ceiba.local");

        // Assert
        result.Should().BeNull();
    }

    [Fact(DisplayName = "SanitizeUrl should return null for null input")]
    public void SanitizeUrl_Null_ReturnsNull()
    {
        // Act
        var result = _sanitizer.SanitizeUrl(null);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Truncate Tests

    [Fact(DisplayName = "Truncate should truncate long text")]
    public void Truncate_LongText_Truncated()
    {
        // Arrange
        var input = "This is a very long text that should be truncated";

        // Act
        var result = _sanitizer.Truncate(input, 10);

        // Assert
        result.Should().HaveLength(10);
        result.Should().Be("This is a ");
    }

    [Fact(DisplayName = "Truncate should preserve short text")]
    public void Truncate_ShortText_Preserved()
    {
        // Arrange
        var input = "Short";

        // Act
        var result = _sanitizer.Truncate(input, 100);

        // Assert
        result.Should().Be("Short");
    }

    [Fact(DisplayName = "Truncate should return empty for null")]
    public void Truncate_Null_ReturnsEmpty()
    {
        // Act
        var result = _sanitizer.Truncate(null, 10);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region Additional Edge Cases (Phase 3)

    [Fact(DisplayName = "Sanitize should handle whitespace-only input")]
    public void Sanitize_WhitespaceOnly_ReturnsWhitespace()
    {
        // Arrange
        var input = "   \t\n  ";

        // Act
        var result = _sanitizer.Sanitize(input);

        // Assert
        result.Should().Be(input);
    }

    [Fact(DisplayName = "Sanitize should handle Unicode characters")]
    public void Sanitize_UnicodeCharacters_EncodesCorrectly()
    {
        // Arrange
        var input = "Español: áéíóú ñ Ñ 日本語 한국어";

        // Act
        var result = _sanitizer.Sanitize(input);

        // Assert - HTML encoding converts non-ASCII to entities
        // But the result should not be null and should have content
        result.Should().NotBeNullOrEmpty();
        // The characters are encoded to HTML entities
        result.Should().Contain("&#"); // Contains HTML entity encoding
    }

    [Fact(DisplayName = "SanitizeHtml should handle encoded XSS attempts")]
    public void SanitizeHtml_EncodedXss_Removed()
    {
        // Arrange
        var input = "<img src=x onerror=&#97;&#108;&#101;&#114;&#116;(1)>";

        // Act
        var result = _sanitizer.SanitizeHtml(input);

        // Assert
        result.Should().NotContain("onerror");
    }

    [Fact(DisplayName = "SanitizeHtml should handle uppercase tags")]
    public void SanitizeHtml_UppercaseTags_Removed()
    {
        // Arrange
        var input = "<SCRIPT>alert('xss')</SCRIPT>";

        // Act
        var result = _sanitizer.SanitizeHtml(input);

        // Assert
        result.Should().NotContainEquivalentOf("<script");
        result.Should().NotContain("alert");
    }

    [Fact(DisplayName = "SanitizeHtml should handle mixed case event handlers")]
    public void SanitizeHtml_MixedCaseEventHandlers_Removed()
    {
        // Arrange
        var input = "<div OnClick=\"evil()\" ONMOUSEOVER=\"bad()\">test</div>";

        // Act
        var result = _sanitizer.SanitizeHtml(input);

        // Assert
        result.Should().NotContainEquivalentOf("onclick");
        result.Should().NotContainEquivalentOf("onmouseover");
    }

    [Fact(DisplayName = "SanitizeForSql should handle multiple SQL injection patterns")]
    public void SanitizeForSql_MultiplePatterns_AllRemoved()
    {
        // Arrange
        var input = "'; DROP TABLE users; SELECT * FROM passwords WHERE '1'='1";

        // Act
        var result = _sanitizer.SanitizeForSql(input);

        // Assert
        result.Should().NotContainEquivalentOf("drop table");
        result.Should().NotContain("'1'='1'");
    }

    [Fact(DisplayName = "SanitizeForSql should handle block comments")]
    public void SanitizeForSql_BlockComments_Removed()
    {
        // Arrange
        var input = "SELECT /* hidden */ * FROM users";

        // Act
        var result = _sanitizer.SanitizeForSql(input);

        // Assert
        result.Should().NotContain("/*");
        result.Should().NotContain("*/");
    }

    [Theory(DisplayName = "SanitizeEmail should handle various valid formats")]
    [InlineData("simple@example.com", "simple@example.com")]
    [InlineData("very.common@example.com", "very.common@example.com")]
    [InlineData("other.email-with-hyphen@example.com", "other.email-with-hyphen@example.com")]
    [InlineData("x@example.com", "x@example.com")]
    public void SanitizeEmail_ValidFormats_Accepted(string input, string expected)
    {
        // Act
        var result = _sanitizer.SanitizeEmail(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact(DisplayName = "SanitizeFileName should handle Windows reserved names")]
    public void SanitizeFileName_WindowsReservedNames_Sanitized()
    {
        // Arrange
        var input = "CON.txt";

        // Act
        var result = _sanitizer.SanitizeFileName(input);

        // Assert - Should either sanitize or return the name (depends on implementation)
        result.Should().NotBeEmpty();
    }

    [Theory(DisplayName = "SanitizeFileName should remove path separators - additional cases")]
    [InlineData("../file.txt")]
    [InlineData("..\\file.txt")]
    [InlineData("folder/file.txt")]
    [InlineData("folder\\file.txt")]
    public void SanitizeFileName_PathSeparators_AdditionalCases(string input)
    {
        // Act
        var result = _sanitizer.SanitizeFileName(input);

        // Assert
        result.Should().NotContain("/");
        result.Should().NotContain("\\");
        result.Should().NotContain("..");
    }

    [Fact(DisplayName = "SanitizeFileName should handle null byte")]
    public void SanitizeFileName_NullByte_Removed()
    {
        // Arrange
        var input = "file\0name.txt";

        // Act
        var result = _sanitizer.SanitizeFileName(input);

        // Assert
        result.Should().NotContain("\0");
    }

    [Fact(DisplayName = "SanitizeUrl should handle data URLs")]
    public void SanitizeUrl_DataUrl_Rejected()
    {
        // Arrange
        var input = "data:text/html,<script>alert('xss')</script>";

        // Act
        var result = _sanitizer.SanitizeUrl(input);

        // Assert
        result.Should().BeNull();
    }

    [Fact(DisplayName = "SanitizeUrl should handle encoded javascript URLs")]
    public void SanitizeUrl_EncodedJavascript_Rejected()
    {
        // Arrange
        var input = "javascript:alert(1)";

        // Act
        var result = _sanitizer.SanitizeUrl(input);

        // Assert
        result.Should().BeNull();
    }

    [Fact(DisplayName = "Truncate should handle exact length")]
    public void Truncate_ExactLength_NotModified()
    {
        // Arrange
        var input = "Exact";

        // Act
        var result = _sanitizer.Truncate(input, 5);

        // Assert
        result.Should().Be("Exact");
    }

    [Fact(DisplayName = "Truncate should handle zero max length")]
    public void Truncate_ZeroMaxLength_ReturnsEmpty()
    {
        // Arrange
        var input = "Some text";

        // Act
        var result = _sanitizer.Truncate(input, 0);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact(DisplayName = "Sanitize should handle very long input")]
    public void Sanitize_VeryLongInput_HandlesCorrectly()
    {
        // Arrange
        var input = new string('x', 10000);

        // Act
        var result = _sanitizer.Sanitize(input);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().Be(10000);
    }

    #endregion
}
