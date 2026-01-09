using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Models;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Controllers;

/// <summary>
/// Endpoint used to perform persisted silent refresh (renew auth cookie) in a context where headers can be written.
/// </summary>
[Route("authentication")]
public class TokenRefreshController(OidcCookieTokenRefresher refresher, IOptions<OidcTokenRefreshOptions> options) : Controller
{
    /// <summary>
    /// Refreshes the access token (if needed) and renews the authentication cookie.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        if (options.Value.Strategy != OidcTokenRefreshStrategy.Persisted)
            return NoContent();

        var refreshed = await refresher.TryRefreshAndRenewCookieAsync(HttpContext, cancellationToken);
        return refreshed ? Ok() : NoContent();
    }
}

