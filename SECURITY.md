# Security Policy

## Supported Versions

The following versions of **ArcGIS Monitor Excel Reporter** currently receive security updates.
Versions follow the `yyyy.MM.dd.BuildNumber` format.

| Version | Supported |
|---------|-----------|
| 2026.x.x.x (latest) | ✅ Active support |
| 2025.x.x.x | ⚠️ Critical fixes only |
| < 2025 | ❌ No longer supported |

> **Recommendation:** Always use the latest release available on the
> [GitHub Releases](https://github.com/desarrollosig-net/ArcGISMonitorExcelReporter/releases) page.

---

## Security Considerations

### Credentials in `config.json`

The application reads credentials (username and password) from a local `config.json` file.

**Best practices:**
- **Never commit `config.json` to version control.** The repository's `.gitignore` already excludes it.
- Store `config.json` outside the application directory when possible, passing its path as a command-line argument.
- Use `"password_encoding": true` to store the password as a Base64-encoded string instead of plain text:
  ```json
  {
    "server": {
      "username": "admin",
      "password": "bXlwYXNzd29yZA==",
      "password_encoding": true
    }
  }
  ```
  > Note: Base64 is **encoding, not encryption**. It prevents accidental exposure in logs or
  > screenshots, but should not be treated as a secure secret store. Use OS-level secret
  > management (e.g., Windows Credential Manager, Linux Keyring, or a secrets vault) for
  > higher-security environments.

### SSL/TLS

- `"ignore_ssl_errors": false` is strongly recommended for all production deployments.
- Set `"ignore_ssl_errors": true` **only** in isolated development or test environments with
  self-signed certificates. Never use this setting against production ArcGIS Monitor instances.

### Network Access

- The application communicates exclusively with the ArcGIS Monitor REST API over HTTP/HTTPS.
- No data is transmitted to third-party services.
- All HTTP requests use a configurable timeout (`timeout_seconds`, default 300 s).

### Output Files

- Generated `.xlsx` reports may contain sensitive monitoring data (metric values, component
  names, alert details). Treat them as confidential and apply appropriate file-system permissions.

---

## Reporting a Vulnerability

If you discover a security vulnerability in this project, please **do not open a public GitHub Issue**.

### Preferred method — GitHub Private Vulnerability Reporting

1. Go to the [Security tab](https://github.com/desarrollosig-net/ArcGISMonitorExcelReporter/security) of the repository.
2. Click **"Report a vulnerability"**.
3. Fill in the description, affected versions, and reproduction steps.

### Alternative — Direct contact

Open a **private** message or email via the contact information on
[github.com/desarrollosig-net](https://github.com/desarrollosig-net).

### What to include

Please provide as much of the following as possible:

- A clear description of the vulnerability and its potential impact.
- Affected version(s) (e.g., `2026.06.30.13`).
- Step-by-step reproduction instructions.
- Any proof-of-concept code or configuration snippets (sanitized of real credentials).

### Response timeline

| Stage | Target |
|-------|--------|
| Acknowledgement | Within **3 business days** |
| Initial assessment | Within **7 business days** |
| Fix or mitigation | Within **30 days** for critical issues |
| Public disclosure | Coordinated with the reporter after fix is released |

---

## Out of Scope

The following are **not** considered vulnerabilities in this project:

- Issues in the ArcGIS Monitor server or API itself (report those to Esri / Safe Software).
- Weak passwords chosen by the operator in `config.json`.
- Vulnerabilities in third-party dependencies (ClosedXML, Serilog) that have no available fix —
  please report those upstream. We will update dependencies as fixes become available.
- Theoretical attacks that require physical access to the machine running the application.

---

## Dependency Security

This project uses the following key dependencies. Known CVEs in these packages will trigger a
priority update:

| Package | Purpose |
|---------|---------|
| [ClosedXML](https://github.com/ClosedXML/ClosedXML) | Excel file generation |
| [Serilog](https://github.com/serilog/serilog) | Structured logging |
| System.Text.Json (.NET 8) | JSON configuration parsing |

NuGet audit is enabled for this project. Run `dotnet restore` locally to surface any
known vulnerabilities in the dependency tree.

---

*Last updated: June 2026*
