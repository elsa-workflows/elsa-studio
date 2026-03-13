# Login Configuration Guide

## Overview

The Elsa Studio login page now uses configuration-driven branding instead of magic constants. This allows for easy customization of branding elements without code changes.

## Configuration Structure

### appsettings.json

```json
{
  "Branding": {
    "AppName": "Elsa Studio",
    "AppTagline": "Workflow Management", 
    "LogoUrl": "/logo.png",
    "ClientVersion": "3.x",
    "ServerVersion": "3.x"
  }
}
```

### Configuration Properties

| Property | Type | Description | Default Value |
|----------|------|-------------|---------------|
| `AppName` | string | Application name displayed on login page | "Elsa Studio" |
| `AppTagline` | string | Tagline/subtitle displayed below app name | "Workflow Management" |
| `LogoUrl` | string | Path to logo image | "/logo.png" |
| `ClientVersion` | string | Client version displayed on login page | "3.x" |
| `ServerVersion` | string | Server version (currently unused but available) | "3.x" |

## Environment-Specific Configuration

### Development Environment
File: `appsettings.Development.json`

```json
{
  "Branding": {
    "AppName": "Elsa Studio (Development)",
    "AppTagline": "Workflow Management - DEV Environment",
    "ClientVersion": "3.x-dev",
    "ServerVersion": "3.x-dev"
  }
}
```

### Production Environment  
File: `appsettings.Production.json`

```json
{
  "Branding": {
    "AppName": "Enterprise Workflow System",
    "AppTagline": "Production Environment",
    "LogoUrl": "/assets/logo-production.png",
    "ClientVersion": "3.1.0",
    "ServerVersion": "3.1.0"
  }
}
```

## Benefits

### 1. **No Magic Constants**
- Version strings are no longer hardcoded in source files
- Easy to update for releases without touching code

### 2. **Environment-Specific Branding**
- Different logos, names, and versions per environment
- Clear visual distinction between dev/staging/production

### 3. **Deployment Flexibility**
- Configuration can be updated during deployment
- No recompilation needed for branding changes

### 4. **Lightweight Performance**
- Configuration reading is extremely fast (local operation)
- No API calls or heavy service dependencies
- Maintains ultra-fast login page performance

## Usage Examples

### Corporate Branding
```json
{
  "Branding": {
    "AppName": "ACME Workflow Manager",
    "AppTagline": "Powered by Elsa Studio",
    "LogoUrl": "/assets/acme-logo.svg",
    "ClientVersion": "2024.1.0",
    "ServerVersion": "2024.1.0"
  }
}
```

### Multi-Tenant Setup
```json
{
  "Branding": {
    "AppName": "{{TENANT_NAME}} Workflows",
    "AppTagline": "Secure Workflow Management",
    "LogoUrl": "/tenant-assets/{{TENANT_ID}}/logo.png",
    "ClientVersion": "{{BUILD_VERSION}}",
    "ServerVersion": "{{API_VERSION}}"
  }
}
```

### Version Automation
For CI/CD pipelines, you can replace version placeholders:

```bash
# Replace version placeholders during build
sed -i 's/{{BUILD_VERSION}}/'${BUILD_NUMBER}'/g' appsettings.Production.json
sed -i 's/{{API_VERSION}}/'${API_VERSION}'/g' appsettings.Production.json
```

## Migration from Magic Constants

### Before (Magic Constants)
```csharp
private string ClientVersion { get; set; } = "3.0.0";  // ? Hardcoded
private string AppName { get; set; } = "Elsa Studio";   // ? Hardcoded
```

### After (Configuration-Driven)
```csharp
// Configuration is injected and read during component initialization
[Inject] private IConfiguration Configuration { get; set; } = null!;

// Values loaded from appsettings.json with fallbacks
AppName = brandingSection.GetValue<string>("AppName") ?? "Elsa Studio";
ClientVersion = brandingSection.GetValue<string>("ClientVersion") ?? "3.x";
```

## Troubleshooting

### Missing Configuration
If branding configuration is missing, the component falls back to safe defaults:
- AppName: "Elsa Studio"  
- AppTagline: "Workflow Management"
- ClientVersion: "Elsa Studio"
- LogoUrl: "/logo.png"

### Performance Impact
Reading configuration has **minimal performance impact**:
- ? Local operation (no network calls)
- ? Cached by ASP.NET Core configuration system  
- ? Synchronous read (no async overhead)
- ? Maintains ultra-fast login page performance