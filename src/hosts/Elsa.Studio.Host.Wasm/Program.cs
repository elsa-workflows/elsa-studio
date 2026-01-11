using Elsa.Studio.Authentication.ElsaAuth.BlazorWasm.Extensions;
using Elsa.Studio.Authentication.ElsaAuth.HttpMessageHandlers;
using Elsa.Studio.Authentication.ElsaAuth.UI.Extensions;
using Elsa.Studio.Dashboard.Extensions;
using Elsa.Studio.Shell;
using Elsa.Studio.Shell.Extensions;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Core.BlazorWasm.Extensions;
using Elsa.Studio.Extensions;
using Elsa.Studio.Localization.Time;
using Elsa.Studio.Localization.Time.Providers;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Designer.Extensions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Elsa.Studio.Localization.Models;
using Elsa.Studio.Localization.BlazorWasm.Extensions;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm.Extensions;
using Elsa.Studio.Authentication.OpenIdConnect.HttpMessageHandlers;

// Build the host.
var builder = WebAssemblyHostBuilder.CreateDefault(args);
var configuration = builder.Configuration;
var services = builder.Services;

// Register root components.
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.RootComponents.RegisterCustomElsaStudioElements();

// Choose authentication provider.
// Supported values: "OpenIdConnect" (default) or "ElsaAuth".
var authProvider = configuration["Authentication:Provider"];
if (string.IsNullOrWhiteSpace(authProvider))
    authProvider = "OpenIdConnect";

Type authenticationHandler;

if (authProvider.Equals("ElsaAuth", StringComparison.OrdinalIgnoreCase))
{
    // Elsa Identity (username/password against Elsa backend) + login UI at /login.
    services.AddElsaAuth();
    services.AddElsaAuthUI();
    authenticationHandler = typeof(ElsaAuthAuthenticatingApiHttpMessageHandler);
}
else if (authProvider.Equals("OpenIdConnect", StringComparison.OrdinalIgnoreCase))
{
    services.AddOpenIdConnectAuth(options =>
    {
        configuration.GetSection("Authentication:OpenIdConnect").Bind(options);
    });
    authenticationHandler = typeof(OidcAuthenticatingApiHttpMessageHandler);
}
else
{
    throw new InvalidOperationException($"Unsupported Authentication:Provider value '{authProvider}'. Supported values are 'OpenIdConnect' and 'ElsaAuth'.");
}

// Register shell services and modules.
var backendApiConfig = new BackendApiConfig
{
    ConfigureBackendOptions = options => configuration.GetSection("Backend").Bind(options),
    ConfigureHttpClientBuilder = options => options.AuthenticationHandler = authenticationHandler,
};

var localizationConfig = new LocalizationConfig
{
    ConfigureLocalizationOptions = options => configuration.GetSection("Localization").Bind(options),
};

services.AddCore();
services.AddShell();
services.AddRemoteBackend(backendApiConfig);

services.AddDashboardModule();
services.AddWorkflowsModule();
services.AddLocalizationModule(localizationConfig);

// Build the application.
var app = builder.Build();

await app.UseElsaLocalization();

// Run each startup task.
var startupTaskRunner = app.Services.GetRequiredService<IStartupTaskRunner>();
await startupTaskRunner.RunStartupTasksAsync();

// Run the application.
await app.RunAsync();

