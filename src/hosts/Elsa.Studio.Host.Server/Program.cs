using System.Diagnostics;
using Elsa.Studio.Core.BlazorServer.Extensions;
using Elsa.Studio.Dashboard.Extensions;
using Elsa.Studio.Extensions;
using Elsa.Studio.Localization.Time;
using Elsa.Studio.Localization.Time.Providers;
using Elsa.Studio.Login.BlazorServer.Extensions;
using Elsa.Studio.Login.HttpMessageHandlers;
using Elsa.Studio.Models;
using Elsa.Studio.Secrets;
using Elsa.Studio.Shell.Extensions;
using Elsa.Studio.Webhooks.Extensions;
using Elsa.Studio.WorkflowContexts.Extensions;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Workflows.Designer.Extensions;
using Elsa.Studio.Localization.BlazorServer.Extensions;
using Elsa.Studio.Localization.Models;
using Elsa.Studio.Localization.Options;
using Elsa.Studio.Translations;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Elsa.Studio.Branding;
using Elsa.Studio.Host.Server;

// Build the host.
var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Register Razor services.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor(options =>
{
    // Register the root components.
    options.RootComponents.RegisterCustomElsaStudioElements();
    options.RootComponents.MaxJSRootComponents = 1000;
});

// Register shell services and modules.
var backendApiConfig = new BackendApiConfig
{
    ConfigureBackendOptions = options => configuration.GetSection("Backend").Bind(options),
    ConfigureHttpClientBuilder = options =>
    {
        options.AuthenticationHandler = typeof(AuthenticatingApiHttpMessageHandler);
        options.ConfigureHttpClient = (_, client) =>
        {
            // Set a long time out to simplify debugging both Elsa Studio and the Elsa Server backend.
            client.Timeout = TimeSpan.FromHours(1);
        };
    },
};

var localizationConfig = new LocalizationConfig
{
    ConfigureLocalizationOptions = options => configuration.GetSection(LocalizationOptions.LocalizationSection).Bind(options),
};

builder.Services.AddScoped<IBrandingProvider, StudioBrandingProvider>();
builder.Services.AddCore().Replace(new ServiceDescriptor(typeof(IBrandingProvider), typeof(StudioBrandingProvider), ServiceLifetime.Scoped));
builder.Services.AddShell(options => configuration.GetSection("Shell").Bind(options));

builder.Services.AddRemoteBackend(backendApiConfig);
builder.Services.AddLoginModule();
builder.Services.AddDashboardModule();
builder.Services.AddWorkflowsModule();
builder.Services.AddWorkflowContextsModule();
builder.Services.AddWebhooksModule();
builder.Services.AddAgentsModule(backendApiConfig);
builder.Services.AddSecretsModule(backendApiConfig);
builder.Services.AddLocalizationModule(localizationConfig);
builder.Services.AddTranslations();

// Replace some services with other implementations.
builder.Services.AddScoped<ITimeZoneProvider, LocalTimeZoneProvider>();

// Configure SignalR.
builder.Services.AddSignalR(options =>
{
    // Set MaximumReceiveMessageSize:
    options.MaximumReceiveMessageSize = 5 * 1024 * 1000; // 5MB
});

// Build the application.
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseResponseCompression();

    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseElsaLocalization();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// Run the application.
app.Run();