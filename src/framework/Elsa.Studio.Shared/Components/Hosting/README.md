# Elsa Studio Hosting Components

This document explains how to use the reusable hosting components provided in the `Elsa.Studio.Shared` package.

## Overview

The hosting components simplify integration of Elsa Studio by providing reusable:
- CSS link references
- JavaScript library includes  
- Loading screen UI
- Blazor initialization scripts

## For Blazor WebAssembly (index.html)

When creating a Blazor WebAssembly host, you can reference the shared JavaScript initialization module:

```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <title>Elsa Studio</title>
    <base href="/" />
    
    <!-- CSS References -->
    <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
    <link href="_content/CodeBeam.MudBlazor.Extensions/MudExtensions.min.css" rel="stylesheet" />
    <link href="_content/Radzen.Blazor/css/material-base.css" rel="stylesheet">
    <link href="_content/Elsa.Studio.Shell/css/shell.css" rel="stylesheet">
</head>
<body>

    <!-- Loading Screen -->
    <div id="elsa-loading" style="position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: #f5f5f5; display: flex; justify-content: center; align-items: center; z-index: 9999;">
        <div style="text-align: center;">
            <div style="width: 40px; height: 40px; border: 4px solid #e0e0e0; border-top: 4px solid #1976d2; border-radius: 50%; animation: elsa-loading-spin 1s linear infinite; margin: 0 auto 20px;"></div>
            <div id="elsa-loading-text" style="color: #666; font-family: 'Roboto', sans-serif;">Initializing...</div>
        </div>
    </div>

    <div id="app"></div>

    <style>
        @keyframes elsa-loading-spin {
            0% { transform: rotate(0deg); }
            100% { transform: rotate(360deg); }
        }
        .blazor-ready #elsa-loading {
            display: none !important;
        }
    </style>

    <!-- JavaScript Libraries -->
    <script src="_content/BlazorMonaco/jsInterop.js"></script>
    <script src="_content/BlazorMonaco/lib/monaco-editor/min/vs/loader.js"></script>
    <script src="_content/BlazorMonaco/lib/monaco-editor/min/vs/editor/editor.main.js"></script>
    <script src="_content/MudBlazor/MudBlazor.min.js"></script>
    <script src="_content/CodeBeam.MudBlazor.Extensions/MudExtensions.min.js"></script>
    <script src="_content/Radzen.Blazor/Radzen.Blazor.js"></script>
    
    <!-- For authentication support -->
    <script src="_content/Microsoft.AspNetCore.Components.WebAssembly.Authentication/AuthenticationService.js"></script>

    <script src="_framework/blazor.webassembly.js"></script>

    <!-- Elsa Studio Initialization -->
    <script src="_content/Elsa.Studio.Shared/js/elsa-studio-init.js"></script>
    <script>
        function initializeElsaBlazorWasm() {
            ElsaStudio.initializeBlazorWasm();
        }

        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', initializeElsaBlazorWasm);
        } else {
            initializeElsaBlazorWasm();
        }

        setTimeout(() => {
            ElsaStudio.hideLoading();
        }, 5000);
    </script>

</body>
</html>
```

## For Blazor Server (_Host.cshtml)

Similar pattern but using Blazor Server framework:

```cshtml
@page "/"
@using Elsa.Studio.Branding
@using Elsa.Studio.Shell
@using Microsoft.AspNetCore.Components.Web
@namespace YourApp.Pages
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@inject IBrandingProvider BrandingProvider

<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <base href="~/" />
    <title>Elsa Studio</title>
    
    <!-- Branding -->
    <link rel="apple-touch-icon" sizes="180x180" href="@BrandingProvider.AppleTouchIconUrl">
    <link rel="icon" type="image/png" sizes="32x32" href="@BrandingProvider.Favicon32Url">
    <link rel="icon" type="image/png" sizes="16x16" href="@BrandingProvider.Favicon16Url">
    
    <!-- CSS References -->
    <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
    <link href="_content/CodeBeam.MudBlazor.Extensions/MudExtensions.min.css" rel="stylesheet" />
    <link href="_content/Radzen.Blazor/css/material-base.css" rel="stylesheet">
    <link href="_content/Elsa.Studio.Shell/css/shell.css" rel="stylesheet">
    <link href="_content/Elsa.Studio.Workflows.Designer/designer.css" rel="stylesheet">

    <component type="typeof(HeadOutlet)" render-mode="Server" />
</head>
<body>

    <!-- Loading Screen -->
    <div id="elsa-loading" style="position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: #f5f5f5; display: flex; justify-content: center; align-items: center; z-index: 9999;">
        <div style="text-align: center;">
            <div style="width: 40px; height: 40px; border: 4px solid #e0e0e0; border-top: 4px solid #1976d2; border-radius: 50%; animation: elsa-loading-spin 1s linear infinite; margin: 0 auto 20px;"></div>
            <div style="color: #666; font-family: 'Roboto', sans-serif;">Initializing...</div>
        </div>
    </div>

    <component type="typeof(App)" render-mode="Server" />

    <div id="blazor-error-ui">
        An error has occurred.
        <a href="" class="reload">Reload</a>
        <a class="dismiss">🗙</a>
    </div>

    <style>
        @@keyframes elsa-loading-spin {
            0% { transform: rotate(0deg); }
            100% { transform: rotate(360deg); }
        }
        .blazor-ready #elsa-loading {
            display: none !important;
        }
    </style>

    <!-- JavaScript Libraries -->
    <script src="_content/BlazorMonaco/jsInterop.js"></script>
    <script src="_content/BlazorMonaco/lib/monaco-editor/min/vs/loader.js"></script>
    <script src="_content/BlazorMonaco/lib/monaco-editor/min/vs/editor/editor.main.js"></script>
    <script src="_content/MudBlazor/MudBlazor.min.js"></script>
    <script src="_content/CodeBeam.MudBlazor.Extensions/MudExtensions.min.js"></script>
    <script src="_content/Radzen.Blazor/Radzen.Blazor.js"></script>
    <script src="_framework/blazor.server.js"></script>

    <!-- Elsa Studio Initialization -->
    <script src="_content/Elsa.Studio.Shared/js/elsa-studio-init.js"></script>
    <script>
        ElsaStudio.initializeBlazorServer(10000);
    </script>

</body>
</html>
```

## JavaScript API

The `elsa-studio-init.js` module provides the following functions:

### ElsaStudio.hideLoading()
Hides the loading screen by adding the 'blazor-ready' class to the body element.

### ElsaStudio.updateLoadingText(text)
Updates the loading screen text.
- `text` - The text to display

### ElsaStudio.initializeBlazorWasm(config)
Initializes Blazor WebAssembly with optional configuration.
- `config` - Optional Blazor startup configuration object

### ElsaStudio.initializeBlazorServer(maxWaitMs)
Initializes Blazor Server with a fallback timeout.
- `maxWaitMs` - Maximum milliseconds to wait before hiding loading screen (default: 10000)

### Legacy Compatibility
For backward compatibility, these functions are also available:
- `window.hideAuthLoading()` - Alias for `ElsaStudio.hideLoading()`
- `window.hideWasmLoading()` - Alias for `ElsaStudio.hideLoading()`
- `window.updateLoadingStatus()` - Alias for `ElsaStudio.updateLoadingText()`

## Razor Components (For Pure Blazor Pages)

The following Razor components are available for use in Blazor components (not Razor Pages/CSHTML):

### ElsaStudioHead
Includes all required CSS links.

```razor
@using Elsa.Studio.Shared.Components.Hosting

<ElsaStudioHead />
```

Parameters:
- `IncludeDesignerCss` (bool, default: true) - Whether to include workflow designer CSS

### ElsaStudioScripts
Includes all required JavaScript library references.

```razor
<ElsaStudioScripts Mode="BlazorHostingMode.WebAssembly" />
```

Parameters:
- `Mode` (BlazorHostingMode) - Server or WebAssembly
- `AutoStart` (bool, default: true) - For WebAssembly, whether to auto-start Blazor

### ElsaStudioLoadingScreen
Displays a loading spinner.

```razor
<ElsaStudioLoadingScreen Id="elsa-loading" />
```

Parameters:
- `Id` (string, default: "elsa-loading") - Element ID
- `TextId` (string, default: "elsa-loading-text") - Text element ID  
- `Text` (string, default: "Initializing...") - Initial text

### ElsaStudioInitScript
Includes initialization JavaScript.

```razor
<ElsaStudioInitScript Mode="BlazorHostingMode.Server" />
```

Parameters:
- `Mode` (BlazorHostingMode) - Server or WebAssembly
- `MaxWaitTimeMs` (int, default: 10000) - For Server mode, max wait time
- `SafetyTimeoutMs` (int, default: 5000) - For WebAssembly, safety timeout
- `CustomConfig` (string, optional) - Custom Blazor configuration JavaScript

## Benefits

✅ **Consistent** - Same UI and behavior across all hosts
✅ **Maintainable** - Update once, applies everywhere  
✅ **Reusable** - Package once via NuGet, use in any Elsa Studio integration
✅ **Minimal** - Reduces boilerplate code in host applications

## Example Projects

See the example host projects in the repository:
- `src/hosts/Elsa.Studio.Host.Server` - Blazor Server example
- `src/hosts/Elsa.Studio.Host.Wasm` - Blazor WebAssembly example
- `src/hosts/Elsa.Studio.Host.HostedWasm` - Hosted WebAssembly example
