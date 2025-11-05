# Elsa Studio - Copilot Coding Agent Instructions

## Repository Overview

**Elsa Studio** is a modular, extensible Blazor-based application framework built with MudBlazor. It provides a visual workflow designer and management interface for Elsa Workflows. The repository contains framework libraries, modules, host applications, and NPM packages for React integration.

- **Repository Size**: ~250 projects across framework, modules, bundles, and hosts
- **Languages/Frameworks**: C# (.NET 8 & .NET 9), Blazor (Server & WebAssembly), TypeScript, JavaScript, React
- **Primary UI Framework**: MudBlazor
- **Build System**: .NET SDK + npm/webpack for client assets
- **Target Runtimes**: .NET 8.0 and .NET 9.0 (multi-targeted)

## Critical Build Requirements

### Prerequisites (MUST be installed)
- **.NET 9 SDK** (9.0.306 or later) - Primary requirement
- **.NET 8 SDK** (8.0.x) - For multi-targeting support
- **Node.js** (v20.x or later) with npm (v10.x or later)

### Package Version Dependencies

**CRITICAL**: The build depends on specific versions in `Directory.Packages.props`. Version mismatches will cause restore failures:

- `ElsaVersion` property controls Elsa.Api.Client and related packages from feedz.io
- `MicrosoftVersion` property controls Microsoft.AspNetCore.* and Microsoft.Extensions.* packages
- **Common Issue**: If Elsa packages are not available at the specified version, you'll get NU1102 errors. The latest available version from feedz.io may differ from the version in the file.
- **Workaround**: If restore fails with NU1102 errors about Elsa packages, check the error message for the "Nearest version" and temporarily update `ElsaVersion` in `Directory.Packages.props` to match. This is expected when working with preview versions.
- **Microsoft version dependency**: If you see NU1605 downgrade warnings, ensure `MicrosoftVersion` matches the requirements of the Elsa packages (typically 9.0.9 or later).

## Build Process (Step-by-Step)

**ALWAYS follow this exact sequence** to avoid build failures:

### 1. Build NPM Assets First (REQUIRED before .NET build)

The .NET build has MSBuild pre-build steps that expect these assets to exist, but manual building is more reliable:

```bash
# Build Workflows Designer client assets (REQUIRED)
cd src/modules/Elsa.Studio.Workflows.Designer/ClientLib
npm install --force
npm run build
cd ../../../..

# Build DOM Interop client assets (REQUIRED)
cd src/framework/Elsa.Studio.DomInterop/ClientLib
npm install --force
npm run build
cd ../../../..
```

**Note**: Use `--force` flag with npm install to bypass peer dependency warnings. Build time: ~20-30 seconds total for both.

### 2. Restore and Build .NET Solution

```bash
# Restore dependencies (takes ~15-20 seconds)
dotnet restore Elsa.Studio.sln

# Build the solution (takes ~70-90 seconds)
dotnet build Elsa.Studio.sln --configuration Release
```

**Expected**: 1500+ warnings (mostly XML documentation warnings) but 0 errors. This is normal.

### 3. Running Host Applications

**Blazor Server** (for local development with backend):
```bash
dotnet run --project src/hosts/Elsa.Studio.Host.Server/Elsa.Studio.Host.Server.csproj
```

**Blazor WebAssembly** (standalone):
```bash
dotnet run --project src/hosts/Elsa.Studio.Host.Wasm/Elsa.Studio.Host.Wasm.csproj
```

**Configuration**: Both hosts read from `appsettings.json`. Default backend URL is `https://localhost:5001/elsa/api` (configure in `Backend.Url` setting).

### 4. Testing

**No automated test infrastructure exists in this repository.** Manual testing is required by running the host applications.

## Project Structure

### Root Directory Files
- `Elsa.Studio.sln` - Main solution file (includes all projects)
- `Elsa.Studio-Core.sln` - Core-only solution (subset of main solution)
- `Directory.Build.props` - MSBuild properties for all projects (targeting, versioning, symbols)
- `Directory.Packages.props` - Central Package Management (CPM) with version properties
- `NuGet.Config` - Package sources including feedz.io for Elsa preview packages
- `README.md` - Build instructions and prerequisites
- `.gitignore` - Excludes bin/, obj/, node_modules/, wwwroot/, and build artifacts

### Source Organization (`src/`)

**Framework** (`src/framework/`) - Core infrastructure:
- `Elsa.Studio.Core` - Core abstractions and services
- `Elsa.Studio.Core.BlazorServer` - Blazor Server-specific implementations
- `Elsa.Studio.Core.BlazorWasm` - WebAssembly-specific implementations
- `Elsa.Studio.Shell` - Application shell and navigation
- `Elsa.Studio.Shared` - Shared components and utilities
- `Elsa.Studio.DomInterop` - DOM interop via JS (has ClientLib/ with TypeScript/webpack)
- `Elsa.Studio.Translations` - Localization infrastructure

**Modules** (`src/modules/`) - Feature modules:
- `Elsa.Studio.Workflows` - Workflow management UI
- `Elsa.Studio.Workflows.Core` - Workflow core services
- `Elsa.Studio.Workflows.Designer` - Visual workflow designer (has ClientLib/ with TypeScript/webpack using @antv/x6)
- `Elsa.Studio.Localization` - Localization module with Blazor-specific variants
- `Elsa.Studio.Login` - Authentication with Blazor-specific variants
- `Elsa.Studio.Dashboard` - Dashboard UI
- `Elsa.Studio.Environments` - Environment management
- `Elsa.Studio.Labels` - Label management
- `Elsa.Studio.Security` - Security module
- `Elsa.Studio.UIHints` - UI hint components
- `Elsa.Studio.Counter` - Sample counter module
- `Elsa.Studio.ActivityPortProviders` - Activity port providers

**Bundles** (`src/bundles/`) - Combined packages:
- `Elsa.Studio` - Main bundle combining framework and modules

**Hosts** (`src/hosts/`) - Executable applications:
- `Elsa.Studio.Host.Server` - Blazor Server host
- `Elsa.Studio.Host.Wasm` - Blazor WebAssembly host
- `Elsa.Studio.Host.HostedWasm` - Hosted WebAssembly (Server + WASM)
- `Elsa.Studio.Host.CustomElements` - Custom Elements for embedding (publishes to WASM npm package)

**Wrappers** (`src/wrappers/`) - NPM packages:
- `wrappers/react-wrapper` - React wrapper for Elsa Studio WASM (@elsa-workflows/elsa-studio-wasm-react)
- Uses Lerna for monorepo management

### ClientLib Projects (TypeScript/Webpack)

Two projects contain TypeScript code that must be built with webpack:
1. `src/framework/Elsa.Studio.DomInterop/ClientLib/` - DOM utilities
2. `src/modules/Elsa.Studio.Workflows.Designer/ClientLib/` - Workflow designer using @antv/x6

Structure:
- `package.json` - npm dependencies and build script
- `tsconfig.json` - TypeScript configuration
- `webpack.config.js` - Webpack bundling configuration
- `src/` - TypeScript source files
- Output: Built JS files in parent project's `wwwroot/` directory (gitignored)

## GitHub Actions CI/CD

**Workflow**: `.github/workflows/packages.yml` (Elsa Studio 3 Packages)

**Triggers**: Push to main/vnext/feature branches, releases

**Build Steps** (runs on ubuntu-latest, 30-minute timeout):
1. Sets version based on branch/tag (preview builds use `{base_version}-{prefix}.{run_number}`)
2. Builds npm assets (with `npm install --force`)
3. Restores and builds .NET solution
4. Runs tests with `dotnet test` (currently no tests defined)
5. Packs NuGet packages to `./packages/nuget/`
6. Publishes Custom Elements to WASM
7. Packs npm packages (wasm and react wrapper)
8. Uploads artifacts
9. Publishes to feedz.io (preview) or nuget.org/npmjs.com (releases)

**Package Feeds**:
- NuGet preview: feedz.io (elsa-workflows/elsa-3)
- NuGet stable: nuget.org
- NPM preview: feedz.io
- NPM stable: npmjs.org

## Configuration Files

### Linting/Code Style
- `Elsa.Studio.sln.DotSettings` - ReSharper/Rider settings (line wrapping disabled, custom abbreviations: JS, UI)
- No ESLint config at root (react-wrapper has `.eslintrc.cjs`)
- No global tsconfig (each ClientLib has its own)

### .gitignore Highlights
- Standard .NET ignores (bin/, obj/, packages/)
- `src/**/wwwroot/` - Generated from ClientLib builds
- `src/modules/Elsa.Studio.Workflows.Designer/ClientLib/package-lock.json` - Excluded to avoid conflicts
- `src/framework/Elsa.Studio.DomInterop/ClientLib/package-lock.json` - Excluded to avoid conflicts
- `node_modules/` - Standard npm ignore

## Common Issues and Workarounds

### NuGet Restore Failures (NU1102)
**Symptom**: Unable to find Elsa.Api.Client or Elsa.Labels with specified version
**Cause**: Preview version in `Directory.Packages.props` not yet published to feedz.io
**Solution**: Update `ElsaVersion` property to nearest available version from error message

### Microsoft Package Downgrade Warnings (NU1605)
**Symptom**: Detected package downgrade: Microsoft.Extensions.DependencyInjection.Abstractions
**Cause**: Elsa packages require newer Microsoft versions than specified
**Solution**: Update `MicrosoftVersion` property to match (typically 9.0.9)

### Build Fails Before ClientLib Assets
**Symptom**: Build errors about missing JS/CSS files
**Cause**: MSBuild prebuild steps expect npm build output
**Solution**: Always build ClientLib assets first (see Build Process step 1)

### npm install Peer Dependency Warnings
**Symptom**: Warnings about peer dependencies during npm install
**Solution**: Use `--force` flag as shown in build steps - this is expected and safe

## Making Code Changes

### General Guidelines
1. **Multi-targeting**: Projects target both net8.0 and net9.0. Test both when making framework changes.
2. **Central Package Management**: All package versions in `Directory.Packages.props`. Don't add versions to individual .csproj files.
3. **Documentation**: XML documentation is enforced (GenerateDocumentationFile=true). Expect CS1591 warnings for missing docs.
4. **Nullable Reference Types**: Enabled in all projects. Handle nullability correctly.
5. **ClientLib changes**: After changing TypeScript in ClientLib, run `npm run build` before building .NET solution.

### Module-Specific Notes
- **Workflows.Designer**: Uses @antv/x6 for visual graph rendering. ClientLib contains graph manipulation logic.
- **DomInterop**: Wrapper for browser APIs. Keep TypeScript types aligned with C# interop signatures.
- **Localization**: Supports multiple cultures. Uses resx files and ILocalizationProvider pattern.

### Configuration Changes
- **appsettings.json**: Different hosts have different settings. Server host includes localization config.
- **Backend.Url**: Points to Elsa Workflows backend API. Must be configured for authentication to work.

## Trust These Instructions

These instructions are comprehensive and validated. **Only search for additional information if**:
1. You encounter an error not documented here
2. You need to understand implementation details within a specific module
3. The build process has changed (check git history for recent changes to build files)

For routine tasks (building, running, making changes to existing files), follow these instructions directly without additional exploration.
