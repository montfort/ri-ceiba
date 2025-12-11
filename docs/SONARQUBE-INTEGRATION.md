# SonarQube Cloud Integration Strategy

## Overview

This document describes the strategy for integrating SonarQube Cloud with our CI/CD pipeline and how to programmatically access detected issues for analysis.

## Authentication

SonarQube Cloud uses **Bearer Token Authentication**.

### Token Generation
- **Team plan**: Scoped Organization Tokens (via Managing Organization section)
- **Free plan**: Personal Access Tokens (via Managing Your Account)

### Usage
```bash
Authorization: Bearer <your_token>
```

## API Endpoints

### Base URL
```
https://sonarcloud.io/api/
```

### Issues Search Endpoint
```
GET /api/issues/search
```

**Key Parameters:**

| Parameter | Description | Example |
|-----------|-------------|---------|
| `componentKeys` | Project key (case-sensitive K) | `montfort_ri-ceiba` |
| `organization` | SonarCloud organization | `montfort` |
| `branch` | Branch name | `001-incident-management-system` |
| `types` | Issue types | `BUG,VULNERABILITY,CODE_SMELL` |
| `severities` | Severity levels | `BLOCKER,CRITICAL,MAJOR,MINOR,INFO` |
| `statuses` | Issue statuses | `OPEN,CONFIRMED,REOPENED,RESOLVED,CLOSED` |
| `resolved` | Filter by resolution | `false` (open issues only) |
| `ps` | Page size (max 500) | `100` |
| `p` | Page number | `1` |
| `facets` | Get aggregated counts | `types,severities,rules` |

**Limitations:**
- Maximum 10,000 results per query (even with pagination)
- Rate limiting applies (429 status when exceeded)

### Example Requests

#### Get all open issues
```bash
curl -s "https://sonarcloud.io/api/issues/search?\
componentKeys=montfort_ri-ceiba&\
organization=montfort&\
branch=001-incident-management-system&\
resolved=false&\
ps=100" \
  -H "Authorization: Bearer $SONAR_TOKEN"
```

#### Get issues by type and severity
```bash
curl -s "https://sonarcloud.io/api/issues/search?\
componentKeys=montfort_ri-ceiba&\
organization=montfort&\
types=BUG,VULNERABILITY&\
severities=BLOCKER,CRITICAL&\
resolved=false&\
ps=100" \
  -H "Authorization: Bearer $SONAR_TOKEN"
```

#### Get issue counts (facets)
```bash
curl -s "https://sonarcloud.io/api/issues/search?\
componentKeys=montfort_ri-ceiba&\
organization=montfort&\
resolved=false&\
ps=1&\
facets=types,severities,rules" \
  -H "Authorization: Bearer $SONAR_TOKEN"
```

## Integration Strategies

### Strategy 1: Direct API Access (Recommended)

Create a script that fetches issues directly from SonarCloud API and formats them for analysis.

**Pros:**
- Real-time data
- Full control over filters
- No CI/CD changes required

**Cons:**
- Requires token management
- Subject to rate limits

### Strategy 2: GitHub Check Annotations

SonarQube Cloud can report issues as GitHub Check annotations on PRs.

**Setup:**
1. Install SonarCloud GitHub App
2. Configure in SonarCloud project settings
3. Issues appear as annotations in PR checks

**Accessing via GitHub API:**
```bash
gh api repos/montfort/ri-ceiba/check-runs/{check_run_id}/annotations
```

**Pros:**
- Issues visible directly in GitHub
- Can block PRs based on quality gate

**Cons:**
- Only for PR analysis (not branch analysis)
- Limited to annotation format

### Strategy 3: CI Artifact Export

Export issues to JSON during CI and store as artifact.

**Workflow addition:**
```yaml
- name: Export SonarQube Issues
  run: |
    curl -s "https://sonarcloud.io/api/issues/search?..." \
      -H "Authorization: Bearer ${{ secrets.SONAR_TOKEN }}" \
      -o sonar-issues.json

- name: Upload Issues Artifact
  uses: actions/upload-artifact@v4
  with:
    name: sonar-issues
    path: sonar-issues.json
```

**Pros:**
- Issues archived with each build
- No direct API access needed later

**Cons:**
- Not real-time
- Requires CI run

### Strategy 4: GitHub Code Scanning (Enterprise Only)

With SonarCloud Enterprise plan, security issues can be exported as GitHub Code Scanning alerts.

**Access via:**
```bash
gh api repos/montfort/ri-ceiba/code-scanning/alerts
```

**Pros:**
- Native GitHub integration
- Security-focused view

**Cons:**
- Enterprise plan required
- Security issues only

## Recommended Implementation

### Phase 1: Script-based Access

Create `scripts/sonar/fetch-issues.sh`:

```bash
#!/bin/bash
# Fetch SonarQube Cloud issues for analysis

SONAR_TOKEN="${SONAR_TOKEN:?SONAR_TOKEN required}"
ORG="${SONAR_ORG:-montfort}"
PROJECT="${SONAR_PROJECT:-montfort_ri-ceiba}"
BRANCH="${SONAR_BRANCH:-001-incident-management-system}"

# Fetch all open issues
curl -s "https://sonarcloud.io/api/issues/search?\
componentKeys=${PROJECT}&\
organization=${ORG}&\
branch=${BRANCH}&\
resolved=false&\
ps=500" \
  -H "Authorization: Bearer ${SONAR_TOKEN}" | \
  jq '{
    total: .total,
    issues: [.issues[] | {
      key: .key,
      rule: .rule,
      severity: .severity,
      type: .type,
      component: .component,
      line: .line,
      message: .message,
      effort: .effort,
      debt: .debt
    }]
  }'
```

### Phase 2: CI Integration

Add to `.github/workflows/ci.yml`:

```yaml
  sonar-export:
    name: Export SonarQube Issues
    runs-on: ubuntu-latest
    needs: build-and-test
    if: github.event_name == 'push'

    steps:
      - name: Fetch and Export Issues
        env:
          SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
        run: |
          curl -s "https://sonarcloud.io/api/issues/search?\
          componentKeys=montfort_ri-ceiba&\
          organization=montfort&\
          branch=${{ github.ref_name }}&\
          resolved=false&\
          ps=500" \
            -H "Authorization: Bearer ${SONAR_TOKEN}" \
            -o sonar-issues.json

      - name: Upload Issues
        uses: actions/upload-artifact@v4
        with:
          name: sonar-issues-${{ github.sha }}
          path: sonar-issues.json
          retention-days: 30
```

### Phase 3: Local Analysis Script

Create `scripts/sonar/analyze-issues.ps1` for Windows/Claude Code:

```powershell
# PowerShell script to fetch and format SonarQube issues
param(
    [string]$Type = "all",      # BUG, VULNERABILITY, CODE_SMELL, or all
    [string]$Severity = "all",  # BLOCKER, CRITICAL, MAJOR, MINOR, INFO, or all
    [string]$Branch = "001-incident-management-system"
)

$token = $env:SONAR_TOKEN
if (-not $token) {
    Write-Error "SONAR_TOKEN environment variable required"
    exit 1
}

$baseUrl = "https://sonarcloud.io/api/issues/search"
$params = @(
    "componentKeys=montfort_ri-ceiba",
    "organization=montfort",
    "branch=$Branch",
    "resolved=false",
    "ps=500"
)

if ($Type -ne "all") { $params += "types=$Type" }
if ($Severity -ne "all") { $params += "severities=$Severity" }

$url = "$baseUrl`?$($params -join '&')"
$headers = @{ "Authorization" = "Bearer $token" }

$response = Invoke-RestMethod -Uri $url -Headers $headers
Write-Output $response | ConvertTo-Json -Depth 10
```

## Environment Variables

Add to your local environment or CI secrets:

| Variable | Description | Example |
|----------|-------------|---------|
| `SONAR_TOKEN` | SonarCloud API token | `squ_xxx...` |
| `SONAR_ORG` | Organization key | `montfort` |
| `SONAR_PROJECT` | Project key | `montfort_ri-ceiba` |

## Usage with Claude Code

To analyze SonarQube issues with Claude Code:

1. Set `SONAR_TOKEN` environment variable
2. Run: `./scripts/sonar/fetch-issues.sh > sonar-issues.json`
3. Ask Claude Code to analyze the issues:
   - "Read sonar-issues.json and summarize the issues by type and severity"
   - "What are the critical security issues in the SonarQube report?"
   - "Help me fix the CODE_SMELL issues in DocumentConversionService.cs"

## Test Coverage Integration

SonarQube Cloud can display test coverage metrics when coverage reports are provided during analysis.

### Supported Coverage Tools

| Tool | Parameter | Format |
|------|-----------|--------|
| Coverlet (OpenCover) | `sonar.cs.opencover.reportsPaths` | `coverage.opencover.xml` |
| Visual Studio | `sonar.cs.vscoveragexml.reportsPaths` | `coverage.xml` |
| dotCover | `sonar.cs.dotcover.reportsPaths` | `dotCover.Output.html` |
| Cobertura | N/A (not directly supported) | Convert to OpenCover |

### Our Implementation

We use **Coverlet with OpenCover format** because:
1. Already integrated via `coverlet.collector` NuGet package
2. Cross-platform (works on Linux CI runners)
3. Native SonarQube support via `sonar.cs.opencover.reportsPaths`

### Configuration Files

#### coverlet.runsettings
```xml
<Configuration>
  <Format>opencover,cobertura</Format>
  <!-- Generates both formats for maximum compatibility -->
</Configuration>
```

#### GitHub Actions Workflow (.github/workflows/sonar.yml)
```yaml
- name: Begin SonarQube analysis
  run: |
    dotnet sonarscanner begin \
      /k:"montfort_ri-ceiba" \
      /o:"montfort" \
      /d:sonar.cs.opencover.reportsPaths="**/coverage.opencover.xml"

- name: Run tests with coverage
  run: |
    dotnet test --collect:"XPlat Code Coverage" \
      --settings coverlet.runsettings

- name: End SonarQube analysis
  run: dotnet sonarscanner end
```

### GitHub Secrets Required

Add these secrets in your GitHub repository settings:

| Secret Name | Description | How to Get |
|-------------|-------------|------------|
| `SONAR_TOKEN` | SonarCloud authentication token | [Generate token](https://sonarcloud.io/account/security) |

### Local Testing

To test coverage locally before pushing:

```bash
# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings

# View coverage report (optional)
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.opencover.xml" -targetdir:"coveragereport" -reporttypes:Html
```

### Coverage Exclusions

The following are excluded from coverage analysis:
- `**/Migrations/**` - EF Core migrations
- `**/Tests/**` - Test projects
- `**/Program.cs` - Entry point
- `**/Startup.cs` - Configuration

Configure in SonarScanner:
```
/d:sonar.coverage.exclusions="**/Migrations/**,**/Tests/**,**/Program.cs"
```

### Viewing Coverage in SonarCloud

After a successful analysis:
1. Go to [SonarCloud Dashboard](https://sonarcloud.io/project/overview?id=montfort_ri-ceiba)
2. Click on "Coverage" metric
3. Drill down into packages/files to see line coverage
4. New code coverage is shown separately for quality gate evaluation

## References

- [SonarQube Cloud Web API Documentation](https://docs.sonarsource.com/sonarqube-cloud/advanced-setup/web-api)
- [SonarQube Cloud API Explorer](https://sonarcloud.io/web_api)
- [.NET Test Coverage](https://docs.sonarsource.com/sonarqube-cloud/enriching/test-coverage/dotnet-test-coverage)
- [GitHub Integration](https://docs.sonarsource.com/sonarqube-cloud/managing-your-projects/administering-your-projects/devops-platform-integration/github)
- [Issues in GitHub](https://docs.sonarsource.com/sonarqube-cloud/managing-your-projects/issues/in-devops-platform/github)
