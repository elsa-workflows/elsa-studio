# Azure AD Authentication for Blazor WASM - Implementation Plan

## Problem Statement

The current Blazor WASM OpenID Connect authentication implementation encounters multiple issues when using Azure AD (Microsoft Entra ID) with API scopes:

### Issues Identified

1. **Missing scope in token exchange**: Azure AD requires the `scope` parameter in both authorization and token endpoint requests, but Blazor WASM's authentication framework only sends scopes during authorization.

2. **Multi-resource limitation**: Azure AD v2.0 only allows requesting scopes for ONE resource per token request. Requesting both Microsoft Graph (`https://graph.microsoft.com/User.Read`) and a custom API (`api://xxx/scope`) results in error AADSTS28000.

3. **UserInfo endpoint mismatch**: Azure AD's userinfo endpoint is at `graph.microsoft.com` which requires a Graph API token, but the access token received is for the custom API, causing "Invalid audience" errors.

4. **Authentication state not persisted**: Even when token exchange succeeds, the authentication state isn't stored in sessionStorage, causing users to be redirected to `/authentication/login-failed`.

5. **Framework limitations**: Microsoft's `Microsoft.AspNetCore.Components.WebAssembly.Authentication` library doesn't expose configuration options to:
   - Add parameters to token endpoint requests
   - Disable userinfo endpoint calls
   - Configure multi-resource token acquisition

## Current Workarounds (Fragile)

The current implementation uses JavaScript interception (`auth-interop.js`) to:
- Intercept `XMLHttpRequest.send()` to add scope parameters to token requests
- Intercept userinfo requests to prevent invalid audience errors

**Problems with this approach:**
- Complex and fragile
- Difficult to maintain
- Doesn't solve the root issue of authentication state not being persisted
- May break with framework updates

## Proposed Solutions

### Option 1: Custom RemoteAuthenticationService (Recommended)

**Goal**: Implement a custom authentication service that properly handles Azure AD's requirements.

**Implementation Steps:**

1. **Create custom OIDC client wrapper**
   - File: `src/modules/Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm/Services/AzureAdAuthenticationService.cs`
   - Inherit from `RemoteAuthenticationService<TRemoteAuthenticationState, TAccount, TProviderOptions>`
   - Override token acquisition methods to inject scope parameter
   - Handle multi-resource token acquisition

2. **Implement custom token endpoint handler**
   ```csharp
   public class AzureAdTokenEndpointHandler : DelegatingHandler
   {
       protected override async Task<HttpResponseMessage> SendAsync(
           HttpRequestMessage request,
           CancellationToken cancellationToken)
       {
           // Intercept token endpoint requests
           if (request.RequestUri.PathAndQuery.Contains("/token"))
           {
               // Add scope parameter to request body
               var content = await request.Content.ReadAsStringAsync();
               if (!content.Contains("scope="))
               {
                   var scopes = // get from configuration
                   content += $"&scope={Uri.EscapeDataString(scopes)}";
                   request.Content = new StringContent(content,
                       Encoding.UTF8,
                       "application/x-www-form-urlencoded");
               }
           }
           return await base.SendAsync(request, cancellationToken);
       }
   }
   ```

3. **Create Azure AD-specific options**
   ```csharp
   public class AzureAdOptions : OidcOptions
   {
       /// <summary>
       /// The primary resource for initial authentication (your API).
       /// </summary>
       public string PrimaryResource { get; set; } = string.Empty;

       /// <summary>
       /// Additional resources that may be accessed (e.g., MS Graph).
       /// Tokens for these will be acquired on-demand.
       /// </summary>
       public List<string> AdditionalResources { get; set; } = new();

       /// <summary>
       /// Whether to skip userinfo endpoint call.
       /// Set to true for Azure AD when using API scopes.
       /// </summary>
       public bool SkipUserInfo { get; set; } = true;
   }
   ```

4. **Implement on-demand token acquisition**
   ```csharp
   public interface IAzureAdTokenService
   {
       /// <summary>
       /// Get access token for the primary resource (from initial auth).
       /// </summary>
       Task<string?> GetPrimaryAccessTokenAsync();

       /// <summary>
       /// Acquire token for additional resource using token exchange.
       /// </summary>
       Task<string?> GetAccessTokenForResourceAsync(string resource);
   }
   ```

5. **Configure metadata to skip userinfo**
   - Override metadata retrieval to remove userinfo_endpoint
   - Or implement custom `IAccountClaimsPrincipalFactory` that doesn't call userinfo

6. **Update service registration**
   ```csharp
   services.AddElsaAzureAdAuthentication(options =>
   {
       options.Authority = "https://login.microsoftonline.com/{tenant}/v2.0";
       options.ClientId = "{client-id}";
       options.PrimaryResource = "api://{api-id}/api-scope";
       options.AdditionalResources = new List<string>
       {
           "https://graph.microsoft.com/.default"
       };
       options.SkipUserInfo = true;
   });
   ```

**Pros:**
- Clean, maintainable solution
- Follows framework patterns
- Can handle multi-resource scenarios
- No JavaScript hacks

**Cons:**
- Significant development effort
- Requires deep understanding of RemoteAuthenticationService internals
- May need to replicate some framework functionality

**Estimated Effort**: 3-5 days

---

### Option 2: Backend-for-Frontend (BFF) Pattern

**Goal**: Move authentication concerns to a backend API that handles token acquisition.

**Architecture:**
```
Browser (Blazor WASM)
  ↓ cookie-based auth
Backend API (BFF)
  ↓ OAuth/OIDC with Azure AD
Azure AD + Your API + MS Graph
```

**Implementation Steps:**

1. **Create BFF API project**
   - ASP.NET Core Web API
   - Add `Microsoft.Identity.Web` package
   - Configure Azure AD authentication with API and Graph scopes

2. **Implement token proxy endpoints**
   ```csharp
   [ApiController]
   [Route("api/auth")]
   [Authorize]
   public class AuthController : ControllerBase
   {
       private readonly ITokenAcquisition _tokenAcquisition;

       [HttpGet("token/{resource}")]
       public async Task<IActionResult> GetToken(string resource)
       {
           var scopes = resource switch
           {
               "api" => new[] { "api://{id}/.default" },
               "graph" => new[] { "https://graph.microsoft.com/.default" },
               _ => throw new ArgumentException("Unknown resource")
           };

           var token = await _tokenAcquisition
               .GetAccessTokenForUserAsync(scopes);
           return Ok(new { access_token = token });
       }
   }
   ```

3. **Update Blazor WASM to use cookie authentication**
   - Remove OIDC configuration
   - Use simple cookie-based auth with BFF
   - Request tokens from BFF as needed

4. **Add token caching in BFF**
   - Use `IDistributedCache` for token storage
   - Handle token refresh automatically
   - Return cached tokens when valid

**Pros:**
- Tokens never exposed to browser
- Can handle complex multi-resource scenarios
- Centralized authentication logic
- Works with any SPA framework

**Cons:**
- Requires additional backend service
- More infrastructure to maintain
- Latency for token requests
- Session management complexity

**Estimated Effort**: 2-3 days for BFF + 1 day for WASM updates

---

### Option 3: Hybrid Approach - Blazor Server for Auth

**Goal**: Use Blazor Server for authentication pages, WASM for main app.

**Architecture:**
- Login/callback pages: Blazor Server (can use Microsoft.Identity.Web properly)
- Main application: Blazor WASM (receives tokens from Server)

**Implementation Steps:**

1. **Create Blazor Server authentication module**
   - Separate Blazor Server project for `/authentication/*` routes
   - Use `Microsoft.Identity.Web` for proper Azure AD integration
   - After authentication, serialize tokens to pass to WASM

2. **Token transfer mechanism**
   - Server writes tokens to secure cookie or session
   - WASM reads tokens on load
   - Or use SignalR to push tokens to WASM

3. **Update routing**
   - All `/authentication/*` routes → Blazor Server
   - All other routes → Blazor WASM

**Pros:**
- Leverages proper Azure AD libraries for auth
- Main app remains WASM (offline capable)
- Proven pattern

**Cons:**
- Complex architecture mixing Server and WASM
- Requires Server hosting (can't be static)
- Token transfer security concerns

**Estimated Effort**: 3-4 days

---

### Option 4: Simplify Scope Requirements

**Goal**: Restructure to avoid multi-resource tokens altogether.

**Approach A: Backend handles external APIs**
- WASM only authenticates user (openid, profile, offline_access)
- Backend API uses On-Behalf-Of flow to access Graph/other APIs
- WASM never needs Graph tokens

**Approach B: Separate authentication contexts**
- User authentication: openid/profile only
- API access: client credentials flow (backend)
- Graph access: handled by backend

**Pros:**
- Simplest from WASM perspective
- Clear separation of concerns
- Most secure (backend controls API access)

**Cons:**
- Backend must proxy all external API calls
- May not fit all architectural requirements

**Estimated Effort**: 1-2 days (if architecture permits)

---

## Recommended Approach

**Primary Recommendation: Option 4 (Simplify) + Option 1 (Custom Service) for future**

### Phase 1: Immediate Fix (Simplify)
1. Remove API scopes from Blazor WASM authentication
2. Use only `openid profile offline_access` for user authentication
3. Backend API uses its own credentials or OBO flow for API access
4. This unblocks current development

### Phase 2: Proper Implementation (Custom Service)
1. Implement custom `RemoteAuthenticationService` for Azure AD
2. Support single-resource tokens with proper scope injection
3. Add on-demand token acquisition for additional resources
4. Provide clear documentation and examples

This approach:
- ✅ Unblocks immediately
- ✅ Provides long-term robust solution
- ✅ Maintains WASM benefits
- ✅ Follows security best practices

---

## Technical Details for Option 1 Implementation

### Files to Create/Modify

1. **New Files:**
   ```
   src/modules/Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm/
   ├── Services/
   │   ├── AzureAdAuthenticationService.cs
   │   ├── AzureAdTokenService.cs
   │   └── AzureAdAccountClaimsPrincipalFactory.cs
   ├── Handlers/
   │   └── AzureAdTokenEndpointHandler.cs
   ├── Models/
   │   └── AzureAdOptions.cs
   └── Extensions/
       └── AzureAdServiceCollectionExtensions.cs
   ```

2. **Modify Files:**
   ```
   src/modules/Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm/
   └── Extensions/ServiceCollectionExtensions.cs (keep for generic OIDC)

   src/hosts/Elsa.Studio.Host.Wasm/
   └── Program.cs (update to use Azure AD-specific registration)
   ```

3. **Remove Files:**
   ```
   src/hosts/Elsa.Studio.Host.Wasm/wwwroot/
   └── auth-interop.js (JavaScript workarounds no longer needed)
   ```

### Key Classes

#### AzureAdAuthenticationService
```csharp
public class AzureAdAuthenticationService : RemoteAuthenticationService<
    RemoteAuthenticationState,
    RemoteUserAccount,
    OidcProviderOptions>
{
    private readonly AzureAdOptions _options;

    protected override async Task<RemoteAuthenticationResult<RemoteAuthenticationState>>
        SignInAsync(RemoteAuthenticationContext<RemoteAuthenticationState> context)
    {
        // Override to inject scope parameter into token request
        // Handle userinfo skipping
        // Process multi-resource scenarios
    }
}
```

#### AzureAdTokenService
```csharp
public class AzureAdTokenService : IAzureAdTokenService
{
    private readonly IAccessTokenProvider _tokenProvider;
    private readonly ITokenAcquisition _tokenAcquisition; // custom impl

    public async Task<string?> GetPrimaryAccessTokenAsync()
    {
        // Return access token from authentication
    }

    public async Task<string?> GetAccessTokenForResourceAsync(string resource)
    {
        // Use refresh token to get token for additional resource
        // Implement token exchange for multi-resource scenarios
    }
}
```

#### AzureAdAccountClaimsPrincipalFactory
```csharp
public class AzureAdAccountClaimsPrincipalFactory :
    AccountClaimsPrincipalFactory<RemoteUserAccount>
{
    protected override async ValueTask<ClaimsPrincipal> CreateUserAsync(
        RemoteUserAccount account,
        RemoteAuthenticationUserOptions options)
    {
        // Skip userinfo endpoint call
        // Extract claims from ID token only
        // Azure AD ID tokens contain all necessary user claims
    }
}
```

### Configuration Example

```csharp
// Program.cs
builder.Services.AddElsaAzureAdAuthentication(options =>
{
    // Basic OIDC settings
    options.Authority = configuration["Authentication:OpenIdConnect:Authority"];
    options.ClientId = configuration["Authentication:OpenIdConnect:ClientId"];
    options.AppBaseUrl = configuration["Authentication:OpenIdConnect:AppBaseUrl"];

    // Azure AD-specific settings
    options.PrimaryResource = "api://dda3270c-997e-413a-9175-36b70134547c/elsa-server-api";
    options.AdditionalResources = new List<string>
    {
        "https://graph.microsoft.com/User.Read"
    };
    options.SkipUserInfo = true; // Don't call userinfo endpoint
});

// Usage in components
@inject IAzureAdTokenService TokenService

private async Task CallApiAsync()
{
    var token = await TokenService.GetPrimaryAccessTokenAsync();
    // Use token for API calls
}

private async Task CallGraphAsync()
{
    var graphToken = await TokenService.GetAccessTokenForResourceAsync(
        "https://graph.microsoft.com/User.Read");
    // Use token for Graph calls
}
```

---

## Testing Plan

1. **Unit Tests:**
   - Token endpoint handler adds scope parameter
   - Multi-resource token acquisition logic
   - Claims extraction from ID token

2. **Integration Tests:**
   - Full authentication flow with Azure AD test tenant
   - Token refresh scenarios
   - Multi-resource token acquisition

3. **Manual Testing:**
   - Login/logout flows
   - Token expiration and refresh
   - Network offline scenarios
   - Browser back/forward navigation
   - Deep linking to protected routes

---

## Documentation Requirements

1. **Azure AD App Registration Guide:**
   - Required API permissions
   - Redirect URI configuration
   - Token configuration (optional claims)

2. **Configuration Guide:**
   - appsettings.json structure
   - Environment-specific settings
   - Multi-resource configuration

3. **Migration Guide:**
   - Updating from generic OIDC to Azure AD-specific
   - Breaking changes
   - JavaScript workaround removal

4. **Troubleshooting Guide:**
   - Common error codes (AADSTS28000, AADSTS28003, etc.)
   - Token acquisition failures
   - Scope configuration issues

---

## Security Considerations

1. **Token Storage:**
   - Blazor WASM stores tokens in browser sessionStorage
   - Risk: XSS attacks can access tokens
   - Mitigation: Strict CSP, regular security audits

2. **Scope Validation:**
   - Backend API must validate access token scopes
   - Never trust client to enforce authorization

3. **Token Lifetime:**
   - Configure appropriate token lifetimes
   - Implement proper refresh token rotation
   - Handle token revocation

4. **PKCE:**
   - Always use PKCE for public clients
   - Already implemented in current code

---

## References

- [Microsoft Identity Platform documentation](https://learn.microsoft.com/en-us/entra/identity-platform/)
- [Azure AD OAuth2 authorization code flow](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-auth-code-flow)
- [Secure ASP.NET Core Blazor WebAssembly](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/webassembly/)
- [IETF RFC 8252 - OAuth 2.0 for Native Apps](https://datatracker.ietf.org/doc/html/rfc8252)

---

## Open Questions

1. **Token Exchange**: Should we implement RFC 8693 token exchange for multi-resource scenarios, or use refresh token to get new access tokens?

2. **Fallback**: Should we maintain generic OIDC support alongside Azure AD-specific implementation?

3. **Backend Integration**: How should the backend API validate tokens for multiple potential audiences?

4. **Graph SDK**: Should we provide a pre-configured Graph SDK client that uses the token service?

5. **Offline Support**: How should token refresh work when the app is offline (PWA scenario)?

---

## Success Criteria

- ✅ User can authenticate with Azure AD
- ✅ Access token for primary API is acquired and usable
- ✅ No JavaScript workarounds required
- ✅ Authentication state persists across page refreshes
- ✅ Tokens refresh automatically before expiration
- ✅ Clear error messages for configuration issues
- ✅ Documentation covers common scenarios
- ✅ Works with both single-resource and multi-resource scenarios (via on-demand acquisition)
