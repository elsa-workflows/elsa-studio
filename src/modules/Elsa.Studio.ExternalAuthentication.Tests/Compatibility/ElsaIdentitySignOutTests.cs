using System.Text;
using System.Text.Json;
using Bunit;
using Bunit.TestDoubles;
using Elsa.Studio.Authentication.ElsaIdentity;
using Elsa.Studio.Authentication.ElsaIdentity.Contracts;
using Elsa.Studio.Authentication.ElsaIdentity.Extensions;
using Elsa.Studio.Authentication.ElsaIdentity.Services;
using Elsa.Studio.Authentication.ElsaIdentity.UI;
using Elsa.Studio.Authentication.ElsaIdentity.UI.Components;
using Elsa.Studio.Authentication.ElsaIdentity.UI.Extensions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.ExternalAuthentication.Tests.Compatibility;

/// <summary>
/// The ElsaIdentity (username/password) provider contributes the same app bar user menu as the broker, with a
/// sign-out entry that ends the local session.
/// </summary>
public sealed class ElsaIdentitySignOutTests : BunitContext, IAsyncLifetime
{
    private readonly InMemoryJwtAccessor _tokens = new();
    private readonly IRenderedComponent<MudPopoverProvider> _popoverProvider;

    public ElsaIdentitySignOutTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddElsaIdentityCore();
        Services.AddElsaIdentityUI();
        Services.AddScoped<IAppBarService, DefaultAppBarService>();
        Services.AddSingleton<IJwtAccessor>(_tokens);
        _popoverProvider = Render<MudPopoverProvider>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public async Task SignedInUser_SeesTheirNameInTheAppBar()
    {
        SignIn("alice");

        var menu = await RenderAppBarMenuAsync();

        menu.WaitForAssertion(() => Assert.Contains("alice", menu.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public async Task AnonymousUser_SeesNoUserMenu()
    {
        var menu = await RenderAppBarMenuAsync();

        Assert.Empty(menu.FindAll(".mud-menu"));
    }

    [Fact]
    public async Task SignOut_ClearsTheSessionAndReturnsToTheLoginPage()
    {
        SignIn("alice");
        _tokens.Tokens[TokenNames.IdToken] = "stale-id-token";
        var stateChanges = new List<AuthenticationState>();
        Services.GetRequiredService<AuthenticationStateProvider>().AuthenticationStateChanged +=
            async state => stateChanges.Add(await state);
        var menu = await RenderAppBarMenuAsync();

        menu.Find(".mud-menu button").Click();
        _popoverProvider.WaitForElements(".mud-menu-item")
            .Single(item => item.TextContent.Trim() == "Sign out")
            .Click();

        menu.WaitForAssertion(() => Assert.Empty(menu.FindAll(".mud-menu")));
        Assert.Empty(_tokens.Tokens);
        Assert.False(Assert.Single(stateChanges).User.Identity?.IsAuthenticated);
        var navigation = Assert.Single(Services.GetRequiredService<BunitNavigationManager>().History);
        Assert.Equal("/login", navigation.Uri);
        Assert.True(navigation.Options.ForceLoad);
    }

    [Fact]
    public async Task RefreshCompletingAfterSignOut_DoesNotRestoreTheSession()
    {
        SignIn("alice");
        var refreshResponse = new PausedHandler();
        var refreshTokenService = new ElsaIdentityRefreshTokenService(
            new StaticRemoteBackendAccessor(), _tokens, new StaticHttpClientFactory(refreshResponse));

        var refresh = refreshTokenService.RefreshTokenAsync(CancellationToken.None);
        await Services.GetRequiredService<ISignOutService>().SignOutAsync();
        refreshResponse.Respond("""{ "isAuthenticated": true, "accessToken": "new-access", "refreshToken": "new-refresh" }""");

        Assert.False((await refresh).IsAuthenticated);
        Assert.Empty(_tokens.Tokens);
    }

    private async Task<IRenderedComponent<ElsaIdentityUserMenu>> RenderAppBarMenuAsync()
    {
        await Services.GetServices<IFeature>().OfType<ElsaIdentityUIFeature>().Single().InitializeAsync();
        var element = Assert.Single(Services.GetRequiredService<IAppBarService>().AppBarElements);
        return Render(element.Component).FindComponent<ElsaIdentityUserMenu>();
    }

    private void SignIn(string userName)
    {
        _tokens.Tokens[TokenNames.AccessToken] = CreateJwt(userName);
        _tokens.Tokens[TokenNames.RefreshToken] = "refresh-token";
    }

    private static string CreateJwt(string userName)
    {
        var payload = JsonSerializer.Serialize(new
        {
            name = userName,
            exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
        });
        return $"{Base64Url("{\"alg\":\"none\"}")}.{Base64Url(payload)}.signature";
    }

    private static string Base64Url(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private sealed class PausedHandler : HttpMessageHandler
    {
        private readonly TaskCompletionSource<HttpResponseMessage> _response = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Respond(string json) =>
            _response.SetResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => _response.Task;
    }

    private sealed class StaticHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class StaticRemoteBackendAccessor : IRemoteBackendAccessor
    {
        public Elsa.Studio.Models.RemoteBackend RemoteBackend { get; } = new(new Uri("https://backend.example"));
    }

    private sealed class InMemoryJwtAccessor : IJwtAccessor
    {
        public Dictionary<string, string> Tokens { get; } = new();

        public ValueTask<string?> ReadTokenAsync(string name) => ValueTask.FromResult(Tokens.GetValueOrDefault(name));

        public ValueTask WriteTokenAsync(string name, string token)
        {
            Tokens[name] = token;
            return ValueTask.CompletedTask;
        }

        public ValueTask ClearTokenAsync(string name)
        {
            Tokens.Remove(name);
            return ValueTask.CompletedTask;
        }
    }
}
