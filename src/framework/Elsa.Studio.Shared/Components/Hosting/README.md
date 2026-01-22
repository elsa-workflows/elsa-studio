# Elsa Studio Hosting Components

This document explains how to use the reusable hosting components provided in the `Elsa.Studio.Shared` package.

## Overview

The hosting components simplify integration of Elsa Studio by providing complete, reusable loaders that handle:
- CSS link references
- JavaScript library includes  
- Loading screen UI
- Blazor initialization scripts

## Quick Start - Minimal Integration

### For Blazor Server

Simply add one script tag in your `_Host.cshtml`:

```cshtml
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <base href="~/" />
    <title>Elsa Studio</title>
    <component type="typeof(HeadOutlet)" render-mode="Server" />
</head>
<body>
    <component type="typeof(App)" render-mode="Server" />
    
    <div id="blazor-error-ui">
        An error has occurred.
        <a href="" class="reload">Reload</a>
        <a class="dismiss">🗙</a>
    </div>

    <!-- Elsa Studio complete loader -->
    <script src="_content/Elsa.Studio.Shared/js/elsa-studio-loader-server.js"></script>
</body>
</html>
```

### For Blazor WebAssembly

Simply add one script tag in your `index.html`:

```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <base href="/" />
    <title>Elsa Studio</title>
</head>
<body>
    <div id="app"></div>
    
    <div id="blazor-error-ui">
        An unhandled error has occurred.
        <a href="" class="reload">Reload</a>
        <a class="dismiss">🗙</a>
    </div>

    <!-- Elsa Studio complete loader -->
    <script src="_content/Elsa.Studio.Shared/js/elsa-studio-loader-wasm.js"></script>
</body>
</html>
```

### For Hosted WebAssembly

```cshtml
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <base href="/" />
    <title>Elsa Studio</title>
</head>
<body>
    <div id="app"></div>
    
    <div id="blazor-error-ui">
        An unhandled error has occurred.
        <a href="" class="reload">Reload</a>
        <a class="dismiss">🗙</a>
    </div>

    <!-- Elsa Studio complete loader with custom configuration -->
    <script src="_content/Elsa.Studio.Shared/js/elsa-studio-loader-hosted-wasm.js"></script>
</body>
</html>
```

## What The Loaders Do

The loader scripts automatically:

1. **Inject CSS** - Add all required stylesheet links to the page
2. **Inject Loading Screen** - Create and display the Elsa Studio loading animation
3. **Load JavaScript Libraries** - Dynamically load all required JavaScript dependencies
4. **Initialize Blazor** - Start Blazor with appropriate configuration
5. **Hide Loading Screen** - Remove the loading screen when Blazor is ready

## Available Loaders

### elsa-studio-loader-server.js
Complete loader for Blazor Server applications. Loads:
- MudBlazor, Radzen, CodeBeam extensions
- BlazorMonaco editor
- Elsa Studio Shell and Workflows Designer CSS
- Blazor Server framework
- Initializes with 10-second timeout fallback

### elsa-studio-loader-wasm.js
Complete loader for standalone Blazor WebAssembly applications. Loads:
- MudBlazor, Radzen, CodeBeam extensions
- BlazorMonaco editor
- Elsa Studio Shell CSS
- WebAssembly authentication services
- Blazor WebAssembly framework
- Initializes with 5-second safety timeout

### elsa-studio-loader-hosted-wasm.js
Complete loader for hosted Blazor WebAssembly applications. Includes:
- All features of the WASM loader
- Custom `loadBootResource` configuration for multi-tenant scenarios
- Supports dynamic base path resolution

## JavaScript API

After loading, the `ElsaStudio` global object is available:

### ElsaStudio.hideLoading()
Manually hide the loading screen.

```javascript
ElsaStudio.hideLoading();
```

### ElsaStudio.updateLoadingText(text)
Update the loading screen text.

```javascript
ElsaStudio.updateLoadingText('Loading modules...');
```

## Advanced Customization

If you need more control, you can still use the original approach with individual components:

### Manual CSS Includes

```html
<link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
<link href="_content/Elsa.Studio.Shell/css/shell.css" rel="stylesheet">
```

### Manual Loading Screen

```html
<link href="_content/Elsa.Studio.Shared/css/elsa-loading.css" rel="stylesheet">
<div id="elsa-loading">
    <div>
        <div id="elsa-loading-spinner"></div>
        <div id="elsa-loading-text">Initializing...</div>
    </div>
</div>
```

### Razor Components (For Pure Blazor Pages)

The following Razor components are available for use in Blazor components (not Razor Pages/CSHTML):

```razor
@using Elsa.Studio.Shared.Components.Hosting

<ElsaStudioHead />
<ElsaStudioScripts Mode="BlazorHostingMode.WebAssembly" />
<ElsaStudioLoadingScreen />
<ElsaStudioInitScript Mode="BlazorHostingMode.Server" />
```

## Benefits

✅ **Minimal Integration** - Just one script tag
✅ **Zero Boilerplate** - No CSS, HTML, or JavaScript to maintain in your host
✅ **Automatic Updates** - Loader updates come with Elsa.Studio.Shared package updates
✅ **Consistent** - Same UI and behavior across all integrations  
✅ **Maintainable** - All plumbing code in one reusable location

## Migration from Manual Setup

If you have an existing host with manual CSS/JS includes:

1. Remove all CSS `<link>` tags for MudBlazor, Radzen, and Elsa Studio
2. Remove all JavaScript `<script>` tags for libraries and frameworks
3. Remove inline loading screen HTML and CSS
4. Remove initialization JavaScript code
5. Add single loader script tag: `<script src="_content/Elsa.Studio.Shared/js/elsa-studio-loader-[server|wasm|hosted-wasm].js"></script>`

## Example Projects

See the example host projects in the repository:
- `src/hosts/Elsa.Studio.Host.Server` - Blazor Server example
- `src/hosts/Elsa.Studio.Host.Wasm` - Blazor WebAssembly example
- `src/hosts/Elsa.Studio.Host.HostedWasm` - Hosted WebAssembly example
