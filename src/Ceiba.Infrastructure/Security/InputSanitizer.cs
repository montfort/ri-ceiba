using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Web;

namespace Ceiba.Infrastructure.Security;

/// <summary>
/// Input sanitization service for preventing XSS and injection attacks.
/// T118b: Input sanitization
/// </summary>
public partial class InputSanitizer : IInputSanitizer
{
    // Timeout for regex operations to prevent ReDoS attacks (SonarQube security fix)
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    // Cache for dynamically created regex patterns to avoid recompilation
    private static readonly ConcurrentDictionary<string, Regex> TagRegexCache = new();
    private static readonly ConcurrentDictionary<string, Regex> AttributeRegexCache = new();

    // Dangerous HTML tags and attributes
    private static readonly string[] DangerousTags =
    {
        "script", "iframe", "object", "embed", "form", "input", "button",
        "link", "style", "meta", "base", "applet", "frame", "frameset"
    };

    private static readonly string[] DangerousAttributes =
    {
        "onclick", "ondblclick", "onmousedown", "onmouseup", "onmouseover",
        "onmousemove", "onmouseout", "onkeypress", "onkeydown", "onkeyup",
        "onload", "onerror", "onsubmit", "onreset", "onfocus", "onblur",
        "onchange", "onselect", "javascript:", "vbscript:", "data:"
    };

    /// <summary>
    /// Sanitize a string by removing potentially dangerous content.
    /// </summary>
    public string Sanitize(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var result = input;

        // Remove null bytes
        result = result.Replace("\0", "");

        // HTML encode special characters
        result = HttpUtility.HtmlEncode(result);

        return result;
    }

    /// <summary>
    /// Sanitize HTML content while preserving safe formatting tags.
    /// </summary>
    public string SanitizeHtml(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var result = input;

        // Remove dangerous tags
        foreach (var tag in DangerousTags)
        {
            result = RemoveTagRegex(tag).Replace(result, "");
        }

        // Remove dangerous attributes
        foreach (var attr in DangerousAttributes)
        {
            result = RemoveAttributeRegex(attr).Replace(result, "");
        }

        // Remove javascript: and data: URLs
        result = RemoveJsUrlRegex().Replace(result, "");

        return result;
    }

    /// <summary>
    /// Sanitize input for SQL queries (parameterized queries should still be used).
    /// </summary>
    public string SanitizeForSql(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        // Remove common SQL injection patterns
        var result = input;

        // Remove comments
        result = SqlCommentRegex().Replace(result, "");

        // Remove union/select patterns
        result = SqlUnionRegex().Replace(result, "");

        // Escape single quotes
        result = result.Replace("'", "''");

        return result;
    }

    /// <summary>
    /// Validate and sanitize an email address.
    /// </summary>
    public string? SanitizeEmail(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return null;
        }

        var trimmed = input.Trim().ToLowerInvariant();

        // Basic email format validation
        if (!EmailRegex().IsMatch(trimmed))
        {
            return null;
        }

        return trimmed;
    }

    /// <summary>
    /// Sanitize a file name to prevent path traversal.
    /// </summary>
    public string SanitizeFileName(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return "unnamed";
        }

        // Remove path separators
        var result = input.Replace("/", "").Replace("\\", "").Replace("..", "");

        // Remove invalid file name characters
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            result = result.Replace(c.ToString(), "");
        }

        // Limit length
        if (result.Length > 255)
        {
            result = result[..255];
        }

        return string.IsNullOrEmpty(result) ? "unnamed" : result;
    }

    /// <summary>
    /// Sanitize a URL to prevent open redirects.
    /// </summary>
    public string? SanitizeUrl(string? input, string? allowedHost = null)
    {
        if (string.IsNullOrEmpty(input))
        {
            return null;
        }

        // Only allow relative URLs or URLs to the same host
        if (input.StartsWith('/') && !input.StartsWith("//"))
        {
            return input;
        }

        if (Uri.TryCreate(input, UriKind.Absolute, out var uri))
        {
            if (!string.IsNullOrEmpty(allowedHost) &&
                uri.Host.Equals(allowedHost, StringComparison.OrdinalIgnoreCase))
            {
                return input;
            }
        }

        // Return null for potentially dangerous URLs
        return null;
    }

    /// <summary>
    /// Truncate text to a maximum length.
    /// </summary>
    public string Truncate(string? input, int maxLength)
    {
        if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
        {
            return input ?? string.Empty;
        }

        return input[..maxLength];
    }

    // Regex patterns (compiled at source generation time for performance)
    [GeneratedRegex(@"<\s*script[^>]*>.*?<\s*/\s*script\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex RemoveScriptTagRegex();

    [GeneratedRegex(@"--.*?(\r\n|\n|$)|/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex SqlCommentRegex();

    [GeneratedRegex(@"\b(union\s+select|select\s+.*\s+from|insert\s+into|delete\s+from|drop\s+table|update\s+.*\s+set)\b", RegexOptions.IgnoreCase)]
    private static partial Regex SqlUnionRegex();

    [GeneratedRegex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"(javascript|vbscript|data)\s*:", RegexOptions.IgnoreCase)]
    private static partial Regex RemoveJsUrlRegex();

    /// <summary>
    /// Creates a cached regex for removing HTML tags with timeout protection against ReDoS.
    /// </summary>
    private static Regex RemoveTagRegex(string tag)
    {
        return TagRegexCache.GetOrAdd(tag, t =>
            new Regex(
                $@"<\s*{Regex.Escape(t)}[^>]*>.*?<\s*/\s*{Regex.Escape(t)}\s*>|<\s*{Regex.Escape(t)}[^>]*/?\s*>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.NonBacktracking,
                RegexTimeout));
    }

    /// <summary>
    /// Creates a cached regex for removing HTML attributes with timeout protection against ReDoS.
    /// </summary>
    private static Regex RemoveAttributeRegex(string attr)
    {
        return AttributeRegexCache.GetOrAdd(attr, a =>
            new Regex(
                $@"\s*{Regex.Escape(a)}\s*=\s*['""][^'""]*['""]|\s*{Regex.Escape(a)}\s*=\s*\S+",
                RegexOptions.IgnoreCase | RegexOptions.NonBacktracking,
                RegexTimeout));
    }
}

/// <summary>
/// Interface for input sanitization.
/// </summary>
public interface IInputSanitizer
{
    string Sanitize(string? input);
    string SanitizeHtml(string? input);
    string SanitizeForSql(string? input);
    string? SanitizeEmail(string? input);
    string SanitizeFileName(string? input);
    string? SanitizeUrl(string? input, string? allowedHost = null);
    string Truncate(string? input, int maxLength);
}
