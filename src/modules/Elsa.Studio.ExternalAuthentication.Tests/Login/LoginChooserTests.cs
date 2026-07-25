using Bunit;
using Elsa.Studio.Authentication.Abstractions.Contracts;
using Elsa.Studio.Authentication.Abstractions.Models;
using Elsa.Studio.Authentication.UI.Services;
using Elsa.Studio.ExternalAuthentication.Models;
using Elsa.Studio.ExternalAuthentication.Services;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Xunit;
using LoginPage = Elsa.Studio.Authentication.UI.Pages.Login;

namespace Elsa.Studio.ExternalAuthentication.Tests.Login;

public sealed class LoginChooserTests : BunitContext, IAsyncLifetime
{
    public LoginChooserTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        JSInterop.SetupVoid("mudKeyInterceptor.connect", _ => true).SetVoidResult();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void Ordering_IsStableAcrossLocalAndExternalMethods()
    {
        var methods = LoginMethodChooserState.Order(
        [
            Method("z", "Zulu", "external", 10),
            Method("local", "Elsa account", "local", 0),
            Method("a", "Alpha", "external", 10)
        ]);

        Assert.Equal(["local", "a", "z"], methods.Select(method => method.Key));
    }

    [Fact]
    public void PreferredMethod_IsVisualOnlyAndNeverStartsAutomatically()
    {
        var coordinator = Register(new(
            [Method("contoso", "Contoso", "external", 0, isDefault: true)],
            "contoso"));

        var cut = Render<LoginPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Preferred", cut.Markup);
            Assert.Contains("Sign in with Contoso", cut.Markup);
        });
        Assert.Equal(0, coordinator.ExternalBegins);
    }

    [Fact]
    public void TrustedIconRegistry_UsesOnlyRegistrationsAndFallsBackSafely()
    {
        var registry = new LoginMethodIconRegistry([new BuiltInLoginMethodIconProvider()]);

        Assert.Equal("GitHub", registry.Resolve("github").AccessibleName);
        Assert.Equal("Identity provider", registry.Resolve("https://untrusted.example/icon.svg").AccessibleName);
    }

    [Fact]
    public void InvalidReturnPaths_AreNeverForwarded()
    {
        Assert.Equal("/", LocalReturnPath.Normalize("https://attacker.example"));
        Assert.Equal("/", LocalReturnPath.Normalize("//attacker.example"));
        Assert.Equal("/workflows?tab=active", LocalReturnPath.Normalize("/workflows?tab=active"));
    }

    [Fact]
    public void Chooser_RendersTextFirstLocalAndExternalMethods()
    {
        Register(new(
            [Method("local", "Elsa account", "local", 0), Method("github", "GitHub", "external", 1)],
            null));

        var cut = Render<LoginPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Elsa account", cut.Markup);
            Assert.Contains("Sign in with GitHub", cut.Markup);
            Assert.Contains("aria-label=\"Sign in with GitHub\"", cut.Markup);
        });
    }

    [Fact]
    public void MethodFailure_ReturnsToChooserWithSafeError()
    {
        Register(
            new([Method("contoso", "Contoso", "external", 0)], null),
            throwExternal: true);

        var cut = Render<LoginPage>();
        cut.WaitForAssertion(() => Assert.Contains("Sign in with Contoso", cut.Markup));
        cut.FindAll("button").Single(x => x.TextContent.Contains("Sign in with Contoso", StringComparison.Ordinal)).Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("selected sign-in method is unavailable", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void NoMethods_ShowsSafeUnavailableState()
    {
        Register(new([], null));

        var cut = Render<LoginPage>();

        cut.WaitForAssertion(() => Assert.Contains("No sign-in methods are currently available", cut.Markup));
    }

    [Fact]
    public void SecurityWarning_IsVisibleWhenConfigured()
    {
        Register(
            new([Method("contoso", "Contoso", "external", 0)], null),
            securityWarning: "Credentials are stored in this browser.");

        var cut = Render<LoginPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Credentials are stored in this browser.", cut.Markup);
            Assert.Contains("role=\"status\"", cut.Markup);
        });
    }

    [Fact]
    public void Chooser_UsesNamedLandmarkAndTrustedFallback()
    {
        Register(new(
        [
            Method("contoso", "Contoso", "external", 2, iconId: "https://untrusted.example/icon.svg")
        ], null));

        var cut = Render<LoginPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("elsa-login-heading", cut.Find("main").GetAttribute("aria-labelledby"));
            Assert.Contains("Sign in with Contoso", cut.Markup);
            Assert.DoesNotContain("https://untrusted.example", cut.Markup, StringComparison.Ordinal);
        });
    }

    private FakeCoordinator Register(
        LoginMethodsResponse response,
        bool throwExternal = false,
        string? securityWarning = null)
    {
        var coordinator = new FakeCoordinator(response, throwExternal, securityWarning);
        Services.AddSingleton<IExternalAuthenticationLoginCoordinator>(coordinator);
        Services.AddScoped<ILoginMethodCatalog, ExternalAuthenticationLoginMethodCatalog>();
        Services.AddSingleton<ILoginMethodComponentRegistry>(
            new LoginMethodComponentRegistry(
            [
                new ExternalLoginMethodComponentProvider(),
                new BrokerLocalLoginMethodComponentProvider()
            ]));
        Services.AddSingleton<ILoginMethodIconRegistry>(
            new LoginMethodIconRegistry([new BuiltInLoginMethodIconProvider()]));
        return coordinator;
    }

    private static LoginMethodDescriptor Method(
        string key,
        string name,
        string kind,
        int order,
        bool isDefault = false,
        string iconId = "github") =>
        new(key, key, kind, name, iconId, order, isDefault, $"/external-authentication/authorize/{key}");

    private sealed class FakeCoordinator(
        LoginMethodsResponse response,
        bool throwExternal = false,
        string? securityWarning = null) : IExternalAuthenticationLoginCoordinator
    {
        public int ExternalBegins { get; private set; }
        public string? SecurityWarning => securityWarning;

        public ValueTask<LoginMethodsResponse> DiscoverAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(response);

        public Task BeginExternalAsync(
            LoginMethodDescriptor method,
            string returnPath,
            CancellationToken cancellationToken = default)
        {
            ExternalBegins++;
            return throwExternal ? Task.FromException(new InvalidOperationException()) : Task.CompletedTask;
        }

        public Task BeginLocalAsync(
            string username,
            string password,
            string returnPath,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
