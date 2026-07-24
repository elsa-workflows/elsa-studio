using Bunit;
using Elsa.Studio.ExternalAuthentication.Models;
using Elsa.Studio.ExternalAuthentication.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using LoginPage = Elsa.Studio.ExternalAuthentication.Pages.Login;

namespace Elsa.Studio.ExternalAuthentication.Tests.Login;

public sealed class LoginChooserTests : BunitContext
{
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
    public void AutomaticMethod_RequiresAnExternalUnattemptedDefaultAndSupportsChooserEscape()
    {
        var response = new LoginMethodsResponse([Method("contoso", "Contoso", "external", 0, isDefault: true)], "contoso");

        Assert.Equal("contoso", LoginMethodChooserState.GetAutomaticMethod(response, false, new HashSet<string>())?.Key);
        Assert.Null(LoginMethodChooserState.GetAutomaticMethod(response, true, new HashSet<string>()));
        Assert.Null(LoginMethodChooserState.GetAutomaticMethod(response, false, new HashSet<string> { "contoso" }));
    }

    [Fact]
    public void TrustedIcons_FallBackToTextOnlyPresentation()
    {
        Assert.True(LoginMethodChooserState.IsTrustedIcon("github"));
        Assert.False(LoginMethodChooserState.IsTrustedIcon("https://untrusted.example/icon.svg"));
        Assert.Equal("identity provider", LoginMethodChooserState.GetAccessibleIconLabel("https://untrusted.example/icon.svg"));
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
        Services.AddSingleton<IExternalAuthenticationLoginCoordinator>(new FakeCoordinator(new(
            [Method("local", "Elsa account", "local", 0), Method("github", "GitHub", "external", 1)], null)));

        var cut = Render<LoginPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Elsa account", cut.Markup);
            Assert.Contains("Sign in with GitHub", cut.Markup);
            Assert.Contains("aria-label=\"Sign in with GitHub\"", cut.Markup);
        });
    }

    [Fact]
    public void Chooser_UsesNamedLandmarksAssociatedFieldsAndDecorativeTrustedIconFallbacks()
    {
        Services.AddSingleton<IExternalAuthenticationLoginCoordinator>(new FakeCoordinator(new(
        [
            Method("local", "Elsa account", "local", 0),
            Method("github", "GitHub", "external", 1),
            Method("contoso", "Contoso", "external", 2, iconId: "https://untrusted.example/icon.svg")
        ], null)));

        var cut = Render<LoginPage>();

        cut.WaitForAssertion(() =>
        {
            var main = cut.Find("main");
            Assert.Equal("external-login-heading", main.GetAttribute("aria-labelledby"));
            Assert.Equal("Sign in", cut.Find("#external-login-heading").TextContent.Trim());
            Assert.NotNull(cut.Find("label[for=\"external-authentication-username\"]"));
            Assert.Equal("username", cut.Find("#external-authentication-username").GetAttribute("autocomplete"));
            Assert.NotNull(cut.Find("label[for=\"external-authentication-password\"]"));
            Assert.Equal("current-password", cut.Find("#external-authentication-password").GetAttribute("autocomplete"));

            var externalButtons = cut.FindAll("button[aria-label^=\"Sign in with\"]");
            Assert.Equal(["Sign in with GitHub", "Sign in with Contoso"], externalButtons.Select(button => button.GetAttribute("aria-label")));
            Assert.All(externalButtons, button => Assert.Equal("button", button.GetAttribute("type")));
            Assert.All(externalButtons, button => Assert.Equal("true", button.QuerySelector("[aria-hidden]")?.GetAttribute("aria-hidden")));
            Assert.Contains("identity provider", externalButtons[1].TextContent);
            Assert.DoesNotContain("https://untrusted.example", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void AutomaticFailure_ReturnsToChooserWithSafeError()
    {
        Services.AddSingleton<IExternalAuthenticationLoginCoordinator>(new FakeCoordinator(
            new([Method("contoso", "Contoso", "external", 0, isDefault: true)], "contoso"), throwExternal: true));

        var cut = Render<LoginPage>();

        cut.WaitForAssertion(() => Assert.Contains("selected sign-in method is unavailable", cut.Markup, StringComparison.OrdinalIgnoreCase));
        Assert.Contains("Choose another sign-in method", cut.Markup);
    }

    [Fact]
    public void NoMethods_ShowsSafeUnavailableState()
    {
        Services.AddSingleton<IExternalAuthenticationLoginCoordinator>(new FakeCoordinator(new([], null)));

        var cut = Render<LoginPage>();

        cut.WaitForAssertion(() => Assert.Contains("No sign-in methods are currently available", cut.Markup));
    }

    [Fact]
    public void SecurityWarning_IsVisibleWhenConfigured()
    {
        Services.AddSingleton<IExternalAuthenticationLoginCoordinator>(new FakeCoordinator(
            new([Method("contoso", "Contoso", "external", 0)], null),
            securityWarning: "Credentials are stored in this browser."));

        var cut = Render<LoginPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Credentials are stored in this browser.", cut.Markup);
            Assert.Contains("role=\"status\"", cut.Markup);
        });
    }

    private static LoginMethod Method(string key, string name, string kind, int order, bool isDefault = false, string iconId = "github") =>
        new(key, key, kind, name, iconId, order, isDefault, $"/external-authentication/authorize/{key}");

    private sealed class FakeCoordinator(LoginMethodsResponse response, bool throwExternal = false, string? securityWarning = null) : IExternalAuthenticationLoginCoordinator
    {
        public string? SecurityWarning => securityWarning;
        public ValueTask<LoginMethodsResponse> DiscoverAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(response);
        public Task BeginExternalAsync(LoginMethod method, string returnPath, CancellationToken cancellationToken = default) => throwExternal ? Task.FromException(new InvalidOperationException()) : Task.CompletedTask;
        public Task BeginLocalAsync(string username, string password, string returnPath, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
