# OWASP ZAP Security Scanning Configuration

This directory contains configuration files for OWASP ZAP security scanning (T020d, RS-001 mitigation).

## Files

- **rules.tsv**: ZAP scan rules configuration with OWASP Top 10 2021 coverage
  - FAIL: Critical issues that will fail the build
  - WARN: Issues that generate warnings but don't fail the build
  - INFO: Informational findings
  - IGNORE: False positives or non-applicable rules

## GitHub Actions Workflow

The security scanning workflow (`.github/workflows/security-scan.yml`) runs:

1. **OWASP ZAP Baseline Scan**: On every push and pull request
2. **OWASP ZAP Full Scan**: Weekly on Sundays at 2 AM (scheduled)

## Required Secrets

Configure these in GitHub repository settings:

- `SONAR_TOKEN`: SonarQube authentication token
- `SONAR_HOST_URL`: SonarQube server URL
- `SNYK_TOKEN`: Snyk API token

## Local Testing

To run ZAP scans locally:

```bash
# Pull ZAP Docker image
docker pull ghcr.io/zaproxy/zaproxy:stable

# Run baseline scan
docker run -v $(pwd):/zap/wrk/:rw -t ghcr.io/zaproxy/zaproxy:stable \
  zap-baseline.py -t http://localhost:5000 -r zap_report.html

# Run full scan
docker run -v $(pwd):/zap/wrk/:rw -t ghcr.io/zaproxy/zaproxy:stable \
  zap-full-scan.py -t http://localhost:5000 -r zap_full_report.html
```

## OWASP Top 10 2021 Coverage

| Category | Coverage |
|----------|----------|
| A01:2021 - Broken Access Control | ✅ |
| A02:2021 - Cryptographic Failures | ✅ |
| A03:2021 - Injection | ✅ |
| A04:2021 - Insecure Design | ✅ |
| A05:2021 - Security Misconfiguration | ✅ |
| A06:2021 - Vulnerable and Outdated Components | ✅ |
| A07:2021 - Identification and Authentication Failures | ✅ |
| A08:2021 - Software and Data Integrity Failures | ✅ |
| A09:2021 - Security Logging and Monitoring Failures | ✅ |
| A10:2021 - Server-Side Request Forgery (SSRF) | ✅ |

## Customization

To modify scan rules:

1. Edit `rules.tsv` with rule IDs and thresholds
2. Rule format: `RULE_ID	THRESHOLD	[COMMENT]`
3. Available thresholds: FAIL, WARN, INFO, IGNORE, OFF
4. Find rule IDs: https://www.zaproxy.org/docs/alerts/

## Integration with CI/CD

The workflow automatically:
- Starts the application with test database
- Runs ZAP scans
- Uploads reports as artifacts
- Creates GitHub issues for vulnerabilities
- Comments on pull requests with results

## Reports

Scan reports are available in:
- GitHub Actions artifacts (30-day retention)
- `report_html.html` - HTML formatted report
- `report_json.json` - JSON data for automation
- `report_md.md` - Markdown summary

## References

- [OWASP ZAP Documentation](https://www.zaproxy.org/docs/)
- [ZAP GitHub Actions](https://github.com/zaproxy/action-baseline)
- [OWASP Top 10 2021](https://owasp.org/Top10/)
