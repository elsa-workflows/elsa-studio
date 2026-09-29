using AngleSharp.Dom;
using Bunit;
using Bunit.TestDoubles;
using Elsa.Studio.Authentication.OpenIdConnect.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.ExternalAuthentication.Tests.Compatibility;

/// <summary>
/// Each OpenID Connect hosting model contributes the shared app bar user menu, shown only to a signed-in user.
/// </summary>
public abstract class OpenIdConnectUserMenuTests<TFeature, TMenu> : BunitContext, IAsyncLifetime
    where TFeature : IFeature
    where TMenu : IComponent
{
    private readonly BunitAuthorizationContext _authorization;
    private IRenderedComponent<MudPopoverProvider>? _popoverProvider;

    protected OpenIdConnectUserMenuTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddScoped<IAppBarService, DefaultAppBarService>();
        _authorization = AddAuthorization();
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

    protected static void ConfigureIdentityProvider(OidcOptions options)
    {
        options.Authority = "https://idp.example";
        options.ClientId = "elsa-studio";
    }

    protected virtual void SignIn(string userName) => _authorization.SetAuthorized(userName);

    /// <summary>Renders the app bar component the provider's feature contributes, the way the shell does.</summary>
    protected async Task<IRenderedComponent<TMenu>> RenderAppBarMenuAsync()
    {
        await Services.GetServices<IFeature>().OfType<TFeature>().Single().InitializeAsync();
        var element = Assert.Single(Services.GetRequiredService<IAppBarService>().AppBarElements);
        _popoverProvider = Render<MudPopoverProvider>();
        return Render(element.Component).FindComponent<TMenu>();
    }

    protected IElement FindInOpenMenu(IRenderedComponent<TMenu> menu, string selector)
    {
        menu.Find(".mud-menu button").Click();
        return _popoverProvider!.WaitForElement(selector);
    }
}
