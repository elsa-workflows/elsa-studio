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
    protected const string UserName = "alice";
    private readonly BunitAuthorizationContext _authorization;
    private IRenderedComponent<MudPopoverProvider>? _popoverProvider;

    protected OpenIdConnectUserMenuTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddScoped<IAppBarService, DefaultAppBarService>();
        _authorization = AddAuthorization();
    }

    // The shell initializes features before it renders anything.
    Task IAsyncLifetime.InitializeAsync() => Services.GetServices<IFeature>().OfType<TFeature>().Single().InitializeAsync().AsTask();
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void SignedInUser_SeesTheirNameInTheAppBar()
    {
        SignIn();

        var menu = RenderAppBarMenu();

        menu.WaitForAssertion(() => Assert.Contains(UserName, menu.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public void AnonymousUser_SeesNoUserMenu()
    {
        var menu = RenderAppBarMenu();

        Assert.Empty(menu.FindAll(".mud-menu"));
    }

    protected static void ConfigureIdentityProvider(OidcOptions options)
    {
        options.Authority = "https://idp.example";
        options.ClientId = "elsa-studio";
    }

    protected virtual void SignIn() => _authorization.SetAuthorized(UserName);

    /// <summary>Renders the app bar component the provider's feature contributed, the way the shell does.</summary>
    protected IRenderedComponent<TMenu> RenderAppBarMenu()
    {
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
