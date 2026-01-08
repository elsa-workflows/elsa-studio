using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Models;
using Microsoft.Extensions.Options;

namespace Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Services;

/// <summary>
/// Helps razor components determine whether persisted refresh is enabled.
/// </summary>
public class OidcTokenRefreshOptionsAccessor(IOptions<OidcTokenRefreshOptions> options)
{
    /// <summary>
    /// Gets the configured refresh strategy.
    /// </summary>
    public OidcTokenRefreshStrategy Strategy => options.Value.Strategy;
}

