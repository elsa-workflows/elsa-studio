# Shadow DOM Support for Elsa Studio

This document describes the Shadow DOM support implementation in Elsa Studio, which enables full style encapsulation when embedding Elsa Studio components as custom elements.

## Overview

Shadow DOM provides a way to encapsulate styles and markup within a component, preventing style conflicts between the host application and Elsa Studio components. This is particularly useful when embedding Elsa Studio in applications with existing styles that might conflict.

## Benefits

- **Style Encapsulation**: Prevents style bleed from the host application into Elsa Studio and vice versa
- **Safe Embedding**: Enables safer embedding in diverse frontend environments (Angular, Vue, React, etc.)
- **Modern Web Standards**: Aligns with modern Web Components best practices

## Configuration

Shadow DOM support can be enabled via configuration in your `appsettings.json`:

```json
{
  "ShadowDOM": {
    "Enabled": true
  }
}
```

## Usage

### Enabling Shadow DOM Support

In your `Program.cs`, the Shadow DOM feature is automatically configured based on the configuration setting:

```csharp
// Get shadow DOM configuration
var enableShadowDOM = configuration.GetValue<bool>("ShadowDOM:Enabled", false);

if (enableShadowDOM)
{
    // Register custom elements with Shadow DOM support
    builder.RootComponents.RegisterCustomElementWithShadowDOM<BackendProvider>("elsa-backend-provider-shadow");
    builder.RootComponents.RegisterCustomElementWithShadowDOM<WorkflowDefinitionEditorWrapper>("elsa-workflow-definition-editor-shadow");
    // ... other components
}
else
{
    // Register custom elements without Shadow DOM (original behavior)
    builder.RootComponents.RegisterCustomElement<BackendProvider>("elsa-backend-provider");
    builder.RootComponents.RegisterCustomElement<WorkflowDefinitionEditorWrapper>("elsa-workflow-definition-editor");
    // ... other components
}
```

### Custom Elements

When Shadow DOM is enabled, components are registered with a `-shadow` suffix:

| Regular Custom Element | Shadow DOM Custom Element |
|------------------------|----------------------------|
| `elsa-backend-provider` | `elsa-backend-provider-shadow` |
| `elsa-workflow-definition-editor` | `elsa-workflow-definition-editor-shadow` |
| `elsa-workflow-instance-viewer` | `elsa-workflow-instance-viewer-shadow` |
| `elsa-workflow-instance-list` | `elsa-workflow-instance-list-shadow` |
| `elsa-workflow-definition-list` | `elsa-workflow-definition-list-shadow` |

### HTML Usage

```html
<!-- Without Shadow DOM (styles may conflict) -->
<elsa-workflow-definition-editor definition-id="some-id"></elsa-workflow-definition-editor>

<!-- With Shadow DOM (styles isolated) -->
<elsa-workflow-definition-editor-shadow definition-id="some-id"></elsa-workflow-definition-editor-shadow>
```

## Implementation Details

### TypeScript/JavaScript

The Shadow DOM functionality is implemented in TypeScript modules:

- `shadow-dom.ts`: Core Shadow DOM functions
- Automatic stylesheet injection for Elsa Studio dependencies
- Custom element registration with Shadow DOM support

### C# Interop

The C# side provides:

- `IDomAccessor` interface with Shadow DOM methods
- `DomJsInterop` implementation for JavaScript interop
- `ShadowDOMExtensions` for easy component registration

### Stylesheets

The following stylesheets are automatically injected into Shadow DOM roots:

- `_content/MudBlazor/MudBlazor.min.css`
- `_content/CodeBeam.MudBlazor.Extensions/MudExtensions.min.css`
- `_content/Radzen.Blazor/css/material-base.css`
- `_content/Elsa.Studio.Shell/css/shell.css`
- `_content/Elsa.Studio.Workflows.Designer/designer.css`

## Advanced Usage

### Custom Stylesheet Configuration

You can provide custom stylesheets when setting up Shadow DOM:

```javascript
// JavaScript/TypeScript
setupElsaShadowRoot(element, [
    'path/to/custom-styles.css',
    '_content/MudBlazor/MudBlazor.min.css',
    // ... other stylesheets
]);
```

### Programmatic Shadow DOM Creation

```javascript
// Create a shadow root with Elsa Studio styles
const shadowRoot = setupElsaShadowRoot(element);

// Register a custom element with Shadow DOM
registerBlazorCustomElementWithShadowDOM('my-custom-element', 'MyBlazorComponent');
```

## Browser Support

Shadow DOM is supported in all modern browsers:

- Chrome 53+
- Firefox 63+
- Safari 10+
- Edge 79+

For older browsers, consider using polyfills or falling back to regular custom elements.

## Examples

See `shadow-dom-demo.html` for a complete example demonstrating the difference between regular and Shadow DOM custom elements.

## Troubleshooting

### Common Issues

1. **Styles not loading**: Ensure all required stylesheets are available at the specified paths
2. **Blazor not initializing**: Check that Blazor is loaded before custom elements are used
3. **Component not rendering**: Verify the component is properly registered and the tag name matches

### Debugging

Enable browser developer tools to inspect Shadow DOM elements:

1. Open DevTools
2. Go to Settings (F1)
3. Enable "Show user agent shadow DOM"
4. Inspect the custom element to see its Shadow DOM tree