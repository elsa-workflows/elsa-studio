# Elsa.Studio.Authentication.ElsaAuth.UI

Provides the Elsa Identity login UI for Elsa Studio.

## What you get
- A Blazor login page at `/login`
- A default unauthorized component provider that redirects to `/login?returnUrl=...`
- A simple app-bar login component (optional)

## Usage
### 1) Configure ElsaAuth (Elsa Identity)
This UI module assumes you are using **Elsa Identity** (username/password against the Elsa backend API).

In your host (Server or WASM), register ElsaAuth and the identity flow, then add the UI:

```csharp
// Platform services:
builder.Services.AddElsaAuth();

// Core + Elsa Identity flow:
builder.Services.AddElsaAuthCore().UseElsaIdentityAuth();

// UI (this package):
builder.Services.AddElsaAuthUI();
```

### 2) Switching providers (hosts)
Elsa Studio hosts can switch providers using configuration:

- `Authentication:Provider` = `OpenIdConnect` or `ElsaAuth`
- `Authentication:OpenIdConnect` contains OIDC settings when using `OpenIdConnect`

> Note: You still need to configure the backend URL to point to your Elsa API.
