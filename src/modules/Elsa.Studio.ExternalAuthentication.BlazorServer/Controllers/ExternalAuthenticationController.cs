using System.Security.Cryptography;
using System.Text;
using Elsa.Studio.Authentication.Abstractions.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.ExternalAuthentication.BlazorServer.Services;
using Elsa.Studio.ExternalAuthentication.Client;
using Elsa.Studio.ExternalAuthentication.Models;
using Elsa.Studio.ExternalAuthentication.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Elsa.Studio.ExternalAuthentication.BlazorServer.Controllers;

/// <summary>Confidential Studio Server broker callbacks. Completion codes and refresh tokens never enter browser code.</summary>
[Route("authentication/external")]
public sealed class ExternalAuthenticationController(
    IAnonymousBackendApiClientProvider anonymousBackendApiClientProvider,
    IServerExternalAuthenticationTransactionStore transactionStore,
    ServerExternalAuthenticationStateProvider authenticationStateProvider,
    ExternalAuthenticationClientOptions options) : Controller
{
    private const string SignInPurpose = "sign-in";
    private const string LocalSignInPurpose = "local-sign-in";

    [HttpGet("login/{connectionKey}")]
    [AllowAnonymous]
    public IActionResult Login(string connectionKey, [FromQuery] string? returnPath)
    {
        if (string.IsNullOrWhiteSpace(connectionKey))
            return Redirect(ChooserUrl(returnPath));

        var callbackUri = AbsolutePath(options.CallbackPath);
        var verifier = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(48));
        var state = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        transactionStore.Store(Response, new(state, verifier, LocalReturnPath.Normalize(returnPath), DateTimeOffset.UtcNow.AddMinutes(10)));

        var authorizationUri = BackendUriResolver.Resolve(
            anonymousBackendApiClientProvider.Url,
            $"external-authentication/authorize/{Uri.EscapeDataString(connectionKey)}");
        var redirect = QueryHelpers.AddQueryString(authorizationUri.ToString(), new Dictionary<string, string?>
        {
            ["client_id"] = options.ClientId,
            ["redirect_uri"] = callbackUri,
            ["response_type"] = "code",
            ["code_challenge"] = WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))),
            ["code_challenge_method"] = "S256",
            ["return_path"] = LocalReturnPath.Normalize(returnPath),
            ["state"] = state
        });
        return Redirect(redirect);
    }

    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error, CancellationToken cancellationToken)
    {
        if (!transactionStore.TryTake(Request, Response, out var transaction) ||
            transaction.Purpose is not (SignInPurpose or LocalSignInPurpose) ||
            string.IsNullOrWhiteSpace(state) || !StateMatches(state, transaction.State))
            return Redirect(ChooserUrl("/"));

        var failureCode = transaction.Purpose == LocalSignInPurpose
            ? LoginFailureCodes.SignInFailed
            : null;

        if (!string.IsNullOrWhiteSpace(error))
            return Redirect(ChooserUrl(transaction.ReturnPath, failureCode));

        if (string.IsNullOrWhiteSpace(code))
            return Redirect(ChooserUrl(transaction.ReturnPath, failureCode));

        try
        {
            var api = await anonymousBackendApiClientProvider.GetApiAsync<IExternalAuthenticationBrokerApi>(cancellationToken);
            var tokens = await api.ExchangeAsync(
                new("authorization_code", options.ClientId, AbsolutePath(options.CallbackPath), code, transaction.CodeVerifier),
                BasicAuthorization(),
                cancellationToken);
            var principal = ServerExternalAuthenticationStateProvider.CreatePrincipal(tokens.AccessToken);
            await authenticationStateProvider.SignInAsync(HttpContext, principal, tokens, cancellationToken);
            return LocalRedirect(LocalReturnPath.Normalize(transaction.ReturnPath));
        }
        catch
        {
            return Redirect(ChooserUrl(transaction.ReturnPath, failureCode));
        }
    }

    /// <summary>Starts the broker-local credential flow without putting credentials or a verifier in browser storage.</summary>
    [HttpPost("local-login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LocalLogin([FromForm] string? username, [FromForm] string? password, [FromForm] string? returnPath, CancellationToken cancellationToken)
    {
        var callbackUri = AbsolutePath(options.CallbackPath);
        var verifier = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(48));
        var state = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var transaction = new ServerExternalAuthenticationTransaction(
            state,
            verifier,
            LocalReturnPath.Normalize(returnPath),
            DateTimeOffset.UtcNow.AddMinutes(10),
            LocalSignInPurpose);
        transactionStore.Store(Response, transaction);

        try
        {
            var api = await anonymousBackendApiClientProvider.GetApiAsync<IExternalAuthenticationBrokerApi>(cancellationToken);
            var response = await api.AuthorizeLocalAsync(new(
                options.ClientId,
                callbackUri,
                "code",
                WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))),
                "S256",
                transaction.ReturnPath,
                username ?? string.Empty,
                password ?? string.Empty,
                state), cancellationToken);
            if (!Uri.TryCreate(response.RedirectUri, UriKind.Absolute, out var redirectUri) || !string.Equals(redirectUri.GetLeftPart(UriPartial.Path), callbackUri, StringComparison.Ordinal))
                return Redirect(ChooserUrl(transaction.ReturnPath, LoginFailureCodes.SignInFailed));
            return Redirect(redirectUri.PathAndQuery);
        }
        catch
        {
            return Redirect(ChooserUrl(transaction.ReturnPath, LoginFailureCodes.SignInFailed));
        }
    }

    /// <summary>Completes locally or upstream broker logout, then removes the secure Server cookie.</summary>
    [HttpPost("logout")]
    [Authorize(AuthenticationSchemes = ServerExternalAuthenticationStateProvider.Scheme)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout([FromForm] string? mode, [FromForm] string? returnPath, CancellationToken cancellationToken)
    {
        var safeReturnPath = LocalReturnPath.Normalize(returnPath);
        try
        {
            var accessToken = await authenticationStateProvider.GetAccessTokenAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                var api = await anonymousBackendApiClientProvider.GetApiAsync<IExternalAuthenticationBrokerApi>(cancellationToken);
                var result = await api.LogoutAsync(new(options.ClientId, AbsolutePath(options.LogoutCallbackPath), mode is "upstream" ? "upstream" : "local"), $"Bearer {accessToken}", cancellationToken);
                await HttpContext.SignOutAsync(ServerExternalAuthenticationStateProvider.Scheme);
                if (!result.Completed && TryGetBackendNavigation(result.NavigationUrl, out var navigationUri))
                {
                    transactionStore.Store(Response, new(
                        WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32)),
                        string.Empty,
                        safeReturnPath,
                        DateTimeOffset.UtcNow.AddMinutes(10),
                        "logout"));
                    return Redirect(navigationUri.ToString());
                }
            }
        }
        catch
        {
            // Local sign-out is still safe and must not depend on upstream availability.
        }

        await HttpContext.SignOutAsync(ServerExternalAuthenticationStateProvider.Scheme);
        return LocalRedirect(safeReturnPath);
    }

    [HttpGet("logout-callback")]
    [AllowAnonymous]
    public IActionResult LogoutCallback()
    {
        if (!transactionStore.TryTake(Request, Response, out var transaction) || !string.Equals(transaction.Purpose, "logout", StringComparison.Ordinal))
            return LocalRedirect("/");
        return LocalRedirect(LocalReturnPath.Normalize(transaction.ReturnPath));
    }

    private string AbsolutePath(string path) => $"{Request.Scheme}://{Request.Host}{path}";
    private string ChooserUrl(string? returnPath, string? error = null)
    {
        var query = new Dictionary<string, string?>
        {
            ["choose"] = "true",
            ["returnPath"] = LocalReturnPath.Normalize(returnPath)
        };

        if (!string.IsNullOrWhiteSpace(error))
            query["error"] = error;

        return QueryHelpers.AddQueryString("/login", query);
    }

    private string? BasicAuthorization()
    {
        if (string.IsNullOrWhiteSpace(options.ClientSecret))
            return null;
        return "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.ClientId}:{options.ClientSecret}"));
    }

    private bool TryGetBackendNavigation(string? navigationUrl, out Uri navigationUri)
    {
        navigationUri = default!;
        if (string.IsNullOrWhiteSpace(navigationUrl))
            return false;
        if (!BackendUriResolver.TryResolveSameOrigin(
                anonymousBackendApiClientProvider.Url,
                navigationUrl,
                out var candidate))
            return false;
        navigationUri = candidate;
        return true;
    }

    private static bool StateMatches(string supplied, string expected)
    {
        var suppliedBytes = Encoding.UTF8.GetBytes(supplied);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        return suppliedBytes.Length == expectedBytes.Length && CryptographicOperations.FixedTimeEquals(suppliedBytes, expectedBytes);
    }
}
