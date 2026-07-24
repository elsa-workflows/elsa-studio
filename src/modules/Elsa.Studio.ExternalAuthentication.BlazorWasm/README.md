# Elsa Studio External Authentication: Blazor WebAssembly

This package registers Studio WebAssembly as a public Elsa External Authentication client. It generates browser PKCE and state, exchanges the opaque completion code with the broker, rotates external refresh credentials, and attaches the current Elsa access token to backend API and SignalR connections.

```csharp
builder.Services.AddExternalAuthenticationBroker(options =>
    configuration.GetSection("Authentication:ExternalAuthentication").Bind(options));
```

```json
{
  "Authentication": {
    "Provider": "ExternalAuthentication",
    "ExternalAuthentication": {
      "ClientId": "elsa-studio-wasm",
      "CallbackPath": "/authentication/external/callback",
      "LogoutCallbackPath": "/authentication/external/logout-callback",
      "BrowserStorage": "Memory"
    }
  }
}
```

WebAssembly must not contain a client secret. Callback values must be client-local absolute paths and must resolve to exact absolute URIs within an allowed origin registered by the matching Elsa Authentication Client.

`Memory` is the default and loses credentials on reload, new tab, or tab close. `Session` uses tab-scoped session storage. `Durable` uses local storage beyond the browser session. Either persistent mode emits a startup security warning and increases exposure to browser script compromise.
