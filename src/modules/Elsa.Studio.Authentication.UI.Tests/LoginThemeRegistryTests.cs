using Elsa.Studio.Authentication.UI.Contracts;
using Elsa.Studio.Authentication.UI.Extensions;
using Elsa.Studio.Authentication.UI.Models;
using Elsa.Studio.Authentication.UI.Options;
using Elsa.Studio.Authentication.UI.Services;
using Elsa.Studio.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Studio.Authentication.UI.Tests;

public class LoginThemeRegistryTests
{
    [Fact]
    public void Selects_the_canonical_registration_for_a_case_insensitive_configuration_value()
    {
        var services = CreateServices("WORKFLOW-AURORA");
        services.AddLoginThemeProvider<TestLoginThemeProvider>("workflow-aurora");

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var registry = scope.ServiceProvider.GetRequiredService<ILoginThemeRegistry>();

        Assert.Equal("workflow-aurora", registry.Selected.Id);
        Assert.IsType<TestLoginThemeProvider>(registry.ResolveSelectedProvider());
    }

    [Fact]
    public void Selects_the_application_theme_when_login_is_configured_to_inherit()
    {
        var services = CreateServices(LoginThemeIds.Inherit);
        services.Configure<StudioThemeOptions>(options => options.Theme = "human-automation");
        services.AddLoginThemeProvider<TestLoginThemeProvider>("human-automation");

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var registry = scope.ServiceProvider.GetRequiredService<ILoginThemeRegistry>();

        Assert.Equal("human-automation", registry.Selected.Id);
    }

    [Fact]
    public void Throws_for_duplicate_registrations_without_hiding_them()
    {
        var services = CreateServices("classic");
        services.AddLoginThemeProvider<TestLoginThemeProvider>("classic");

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var exception = Assert.Throws<OptionsValidationException>(
            () => scope.ServiceProvider.GetRequiredService<ILoginThemeRegistry>());

        Assert.Contains("registered more than once", exception.Message);
    }

    [Fact]
    public void Throws_for_an_unknown_selected_theme()
    {
        var services = CreateServices("missing");

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var exception = Assert.Throws<OptionsValidationException>(
            () => scope.ServiceProvider.GetRequiredService<ILoginThemeRegistry>());

        Assert.Contains("no matching login theme", exception.Message);
    }

    [Fact]
    public void AddAuthenticationUI_can_be_called_more_than_once_without_duplicate_classic_registration()
    {
        var services = new ServiceCollection();
        services.AddAuthenticationUI();
        services.AddAuthenticationUI();

        Assert.Single(services, x => x.ServiceType == typeof(LoginThemeRegistration));
    }

    [Fact]
    public void AddAuthenticationUI_does_not_hide_a_pre_registered_classic_theme()
    {
        var services = new ServiceCollection();
        services.AddLoginThemeProvider<TestLoginThemeProvider>("classic");
        services.AddAuthenticationUI();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var exception = Assert.Throws<OptionsValidationException>(
            () => scope.ServiceProvider.GetRequiredService<ILoginThemeRegistry>());

        Assert.Contains("registered more than once", exception.Message);
    }

    private static ServiceCollection CreateServices(string theme)
    {
        var services = new ServiceCollection();
        services.AddAuthenticationUI();
        services.Configure<LoginThemeOptions>(options => options.Theme = theme);
        return services;
    }
}
