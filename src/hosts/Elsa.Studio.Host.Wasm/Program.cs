using Elsa.Studio.Authentication.Abstractions.HttpMessageHandlers;
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

// Build the host.
var builder = WebAssemblyHostBuilder.CreateDefault(args);
var configuration = builder.Configuration;

// Register root components.
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.RootComponents.RegisterCustomElsaStudioElements();

// Register shell services and modules.
var backendApiConfig = new BackendApiConfig
{
    ConfigureBackendOptions = options => configuration.GetSection("Backend").Bind(options),
    ConfigureHttpClientBuilder = options => options.AuthenticationHandler = typeof(AuthenticatingApiHttpMessageHandler), 
};

var localizationConfig = new LocalizationConfig
{
    ConfigureLocalizationOptions = options => configuration.GetSection("Localization").Bind(options),
};

builder.Services.AddCore();
builder.Services.AddShell();
builder.Services.AddRemoteBackend(backendApiConfig);

// Choose authentication provider.
// Supported values: "OpenIdConnect" (default) or "ElsaAuth".
var authProvider = configuration["Authentication:Provider"];
if (string.IsNullOrWhiteSpace(authProvider))
    authProvider = "OpenIdConnect";

authProvider = authProvider.Trim();

if (authProvider.Equals("ElsaAuth", StringComparison.OrdinalIgnoreCase))
{
    // Elsa Identity (username/password against Elsa backend) + login UI at /login.
    Elsa.Studio.Authentication.ElsaAuth.BlazorWasm.Extensions.ServiceCollectionExtensions.AddElsaAuth(builder.Services);
    Elsa.Studio.Authentication.ElsaAuth.UI.Extensions.ServiceCollectionExtensions.AddElsaAuthUI(builder.Services);
}
else if (authProvider.Equals("OpenIdConnect", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddElsaOidcAuthentication(options =>
    {
        configuration.GetSection("Authentication:OpenIdConnect").Bind(options);
    });
}
else
{
    throw new InvalidOperationException($"Unsupported Authentication:Provider value '{authProvider}'. Supported values are 'OpenIdConnect' and 'ElsaAuth'.");
}

builder.Services.AddDashboardModule();
builder.Services.AddWorkflowsModule();
builder.Services.AddLocalizationModule(localizationConfig);

// Replace some services with other implementations.
builder.Services.AddScoped<ITimeZoneProvider, LocalTimeZoneProvider>();

// Build the application.
var app = builder.Build();

await app.UseElsaLocalization();

// Run each startup task.
var startupTaskRunner = app.Services.GetRequiredService<IStartupTaskRunner>();
await startupTaskRunner.RunStartupTasksAsync();

// Run the application.
await app.RunAsync();

