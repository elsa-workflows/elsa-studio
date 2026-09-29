using System.Reflection;
using System.Security.Claims;
using AngleSharp.Dom;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Components;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Controllers;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Extensions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Xunit;

namespace Elsa.Studio.ExternalAuthentication.Tests.Compatibility;

/// <summary>
/// On Blazor Server the OpenID Connect user menu posts an antiforgery-protected sign-out that ends the cookie session
/// and, when the provider advertises an end_session_endpoint, performs RP-initiated logout.
/// </summary>
public sealed class OpenIdConnectBlazorServerSignOutTests
    : OpenIdConnectUserMenuTests<OpenIdConnectBlazorServerFeature, OpenIdConnectUserMenu>
{
    private const string IdToken = "alice-id-token";
    private const string EndSessionEndpoint = "https://idp.example/logout";
    private readonly DefaultHttpContext _page = CreateHttpContext(HttpMethods.Get, "/");
    private readonly OpenIdConnectConfiguration _provider = new() { EndSessionEndpoint = EndSessionEndpoint };

    public OpenIdConnectBlazorServerSignOutTests()
    {
        Services.AddOpenIdConnectAuth(ConfigureIdentityProvider);
        Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options => options.Configuration = _provider);
        Services.AddSingleton<IHttpContextAccessor>(new PageHttpContextAccessor(_page));
        Services.AddSingleton<ISingleFlightCoordinator, SingleFlightCoordinator>();
    }

    [Fact]
    public async Task SignOut_SubmitsARequestTheSignOutEndpointAccepts()
    {
        SignIn("alice");
        var menu = await RenderAppBarMenuAsync();

        var form = FindInOpenMenu(menu, "form");

        Assert.Equal("post", form.GetAttribute("method"));
        Assert.Equal("/authentication/logout", form.GetAttribute("action"));
        Assert.Equal("Sign out", form.QuerySelector("button[type=submit]")?.TextContent.Trim());
        Assert.True(await Services.GetRequiredService<IAntiforgery>().IsRequestValidAsync(Submit(form)));
    }

    [Fact]
    public async Task AnonymousUser_IsNotIssuedAnAntiforgeryCookie()
    {
        await RenderAppBarMenuAsync();

        Assert.Empty(_page.Response.Headers.SetCookie.ToArray());
    }

    [Fact]
    public void SignOutEndpoint_AcceptsOnlyAntiforgeryProtectedPosts()
    {
        var logout = typeof(AuthenticationController).GetMethod(nameof(AuthenticationController.Logout))!;

        Assert.NotNull(logout.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        Assert.Equal([HttpMethods.Post], logout.GetCustomAttributes<HttpMethodAttribute>().SelectMany(attribute => attribute.HttpMethods));
    }

    [Fact]
    public async Task SignOut_EndsTheLocalSessionAndRedirectsToTheProvidersEndSessionEndpoint()
    {
        var response = await SignOutAsync("/workflows");

        AssertSessionCookieDeleted(response);
        var endSession = new Uri(response.Headers.Location.ToString());
        var query = QueryHelpers.ParseQuery(endSession.Query);
        Assert.Equal(StatusCodes.Status302Found, response.StatusCode);
        Assert.Equal(EndSessionEndpoint, endSession.GetLeftPart(UriPartial.Path));
        Assert.Equal(IdToken, query["id_token_hint"]);
        Assert.Equal("https://studio.example/signout-callback-oidc", query["post_logout_redirect_uri"]);
    }

    [Fact]
    public async Task SignOutWithoutAnEndSessionEndpoint_EndsTheLocalSessionOnly()
    {
        _provider.EndSessionEndpoint = null;

        var response = await SignOutAsync("/workflows");

        AssertSessionCookieDeleted(response);
        Assert.Equal(StatusCodes.Status302Found, response.StatusCode);
        Assert.Equal("/workflows", response.Headers.Location.ToString());
    }

    protected override void SignIn(string userName)
    {
        base.SignIn(userName);
        _page.User = CreateUser(userName);
    }

    /// <summary>Runs the sign-out endpoint through the real cookie and OpenID Connect handlers.</summary>
    private async Task<HttpResponse> SignOutAsync(string returnUrl)
    {
        await using var scope = Services.CreateAsyncScope();
        var context = CreateHttpContext(HttpMethods.Post, "/authentication/logout");
        context.RequestServices = scope.ServiceProvider;
        context.Request.Headers.Cookie = SessionCookie(scope.ServiceProvider);

        var result = new AuthenticationController().Logout(returnUrl);
        await result.ExecuteResultAsync(new ActionContext(context, new RouteData(), new ActionDescriptor()));

        return context.Response;
    }

    /// <summary>Builds the POST the browser sends when the form is submitted from the page that rendered it.</summary>
    private HttpContext Submit(IElement form)
    {
        var context = CreateHttpContext(form.GetAttribute("method")!.ToUpperInvariant(), form.GetAttribute("action")!);
        context.User = _page.User;
        context.Request.Headers.Cookie = string.Join("; ", _page.Response.Headers.SetCookie.Select(cookie => cookie!.Split(';')[0]));
        context.Request.ContentType = "application/x-www-form-urlencoded";
        context.Request.Form = new FormCollection(form.QuerySelectorAll("input").ToDictionary(
            input => input.GetAttribute("name")!,
            input => new StringValues(input.GetAttribute("value"))));
        return context;
    }

    private static string SessionCookie(IServiceProvider services)
    {
        var options = services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(CookieAuthenticationDefaults.AuthenticationScheme);
        var properties = new AuthenticationProperties();
        properties.StoreTokens([new AuthenticationToken { Name = "id_token", Value = IdToken }]);
        var ticket = new AuthenticationTicket(CreateUser("alice"), properties, CookieAuthenticationDefaults.AuthenticationScheme);
        return $"{options.Cookie.Name}={options.TicketDataFormat.Protect(ticket)}";
    }

    private static void AssertSessionCookieDeleted(HttpResponse response) =>
        Assert.Contains(response.Headers.SetCookie, cookie =>
            cookie!.StartsWith("ElsaStudio.Auth=;", StringComparison.Ordinal) &&
            cookie.Contains("expires=Thu, 01 Jan 1970", StringComparison.Ordinal));

    private static ClaimsPrincipal CreateUser(string userName) =>
        new(new ClaimsIdentity([new Claim("sub", userName), new Claim("name", userName)], CookieAuthenticationDefaults.AuthenticationScheme, "name", "role"));

    private static DefaultHttpContext CreateHttpContext(string method, string path) => new()
    {
        Request = { Method = method, Scheme = "https", Host = new HostString("studio.example"), Path = path }
    };

    private sealed class PageHttpContextAccessor(HttpContext page) : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; } = page;
    }
}
