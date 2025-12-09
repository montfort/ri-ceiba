# Fix: Gemini API Authentication Failure

## Problem

Automated reports were showing fallback messages despite having Gemini AI configured with successful connection tests. The error logs showed:

```
2025-12-09 00:41:13.501 -06:00 [ERR] Gemini API error: Unauthorized - {
  "error": {
    "message": "Missing bearer or basic authentication in header",
    "type": "invalid_request_error",
    "param": null,
    "code": null
  }
}
2025-12-09 00:41:13.563 -06:00 [ERR] Error generating AI narrative with provider Gemini. Error: Gemini API error: Unauthorized. Using fallback.
```

## Root Cause

The issue was caused by an incorrect endpoint URL in the database configuration:

- **Database `Endpoint` field**: `https://api.openai.com/v1/chat/completions` (OpenAI URL!)
- **Expected Gemini URL**: `https://generativelanguage.googleapis.com/v1beta/models`

### Why Connection Test Passed

The connection test method (`TestGeminiAsync` in `AiConfigurationService.cs`) worked because it **ignored** the configured `Endpoint` field and always used the hardcoded correct Gemini URL:

```csharp
// AiConfigurationService.cs:366
const string geminiBaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";
var endpoint = $"{geminiBaseUrl}/{config.Modelo}:generateContent";
```

### Why Narrative Generation Failed

The narrative generation method (`CallGeminiAsync` in `AiNarrativeService.cs`) **used** the configured `Endpoint` field from the database:

```csharp
// AiNarrativeService.cs:366 (before fix)
var baseEndpoint = config.Endpoint ?? "https://generativelanguage.googleapis.com/v1beta/models";
var endpoint = $"{baseEndpoint}/{config.Modelo}:generateContent";
```

This resulted in requests being sent to OpenAI's endpoint with Gemini's authentication format (`x-goog-api-key` header), which OpenAI rejected.

## Solution

### 1. Code Fix

Updated `AiNarrativeService.cs` to match the behavior of `AiConfigurationService.cs` by always using the correct Gemini base URL:

```csharp
// AiNarrativeService.cs:366-368 (after fix)
// Gemini API endpoint - always use the correct base URL (ignore config.Endpoint)
const string geminiBaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";
var endpoint = $"{geminiBaseUrl}/{config.Modelo}:generateContent";
```

### 2. Database Update

Corrected the `Endpoint` field in the database:

```sql
UPDATE "CONFIGURACION_IA"
SET "Endpoint" = 'https://generativelanguage.googleapis.com/v1beta/models'
WHERE "Proveedor" = 'Gemini';
```

## Files Changed

- **`src/Ceiba.Infrastructure/Services/AiNarrativeService.cs`**:
  - Line 366-368: Changed to use hardcoded Gemini base URL instead of `config.Endpoint`
  - Added comment explaining the fix

## Testing

After applying the fix:

1. ✅ Build successful
2. ⏳ User should test: Generate a new automated report
3. ⏳ User should verify: AI-generated narrative appears instead of fallback message
4. ⏳ User should check: Terminal logs show successful Gemini API calls

## Prevention

This issue occurred because the UI allows users to configure the `Endpoint` field for Gemini, but:

1. Gemini has a **fixed** API endpoint that should not be configurable
2. The connection test and narrative generation were using different logic

**Recommendation**: Consider hiding or making the `Endpoint` field read-only for Gemini provider in the UI, since it should always be the same value.

## Related Issues

- Initial report filtering fix: Reports now only include Estado = 1 (Entregados)
- Markdown table rendering fix: Tables now render as HTML with proper styling
- Statistics redundancy fix: Removed duplicate "Entregados" vs "Total" distinctions
- AI prompt optimization: Simplified to avoid redundant messaging

## Date

2025-12-09
