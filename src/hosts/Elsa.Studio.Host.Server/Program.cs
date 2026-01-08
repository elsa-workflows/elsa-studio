using Elsa.Studio.Branding;
using Elsa.Studio.Contracts;
using Elsa.Studio.Core.BlazorServer.Extensions;
using Elsa.Studio.Dashboard.Extensions;
using Elsa.Studio.Extensions;
using Elsa.Studio.Host.Server;
using Elsa.Studio.Localization.BlazorServer.Extensions;
using Elsa.Studio.Localization.Models;
using Elsa.Studio.Localization.Options;
using Elsa.Studio.Localization.Time;
using Elsa.Studio.Localization.Time.Providers;
using Elsa.Studio.Login.BlazorServer.Extensions;
using Elsa.Studio.Login.Extensions;
using Elsa.Studio.Login.HttpMessageHandlers;
using Elsa.Studio.Models;
using Elsa.Studio.Shell.Extensions;
using Elsa.Studio.Translations;
using Elsa.Studio.Workflows.ActivityPickers.Treeview;
using Elsa.Studio.Workflows.Designer.Extensions;
using Elsa.Studio.Workflows.Extensions;
using Microsoft.Extensions.DependencyInjection.Extensions;

// Build the host.
var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Register Razor services.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor(options =>
{
    // Register the root components.
    // V2 activity wrapper by default.
    options.RootComponents.RegisterCustomElsaStudioElements();

    // To use V1 activity wrapper layout, specify the V1 component instead:
    //options.RootComponents.RegisterCustomElsaStudioElements(typeof(Elsa.Studio.Workflows.Designer.Components.ActivityWrappers.V1.EmbeddedActivityWrapper));

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
    ConfigureLocalizationOptions = options =>
    {
        configuration.GetSection(LocalizationOptions.LocalizationSection).Bind(options);
        options.SupportedCultures = new[] { options?.DefaultCulture ?? new LocalizationOptions().DefaultCulture }
            .Concat(options?.SupportedCultures.Where(culture => culture != options?.DefaultCulture) ?? []).ToArray();
    }
};

builder.Services.AddScoped<IBrandingProvider, StudioBrandingProvider>();
builder.Services.AddCore().Replace(new(typeof(IBrandingProvider), typeof(StudioBrandingProvider), ServiceLifetime.Scoped));
builder.Services.AddShell(options => configuration.GetSection("Shell").Bind(options));
builder.Services.AddRemoteBackend(backendApiConfig);
builder.Services.AddLoginModule();
//builder.Services.UseElsaIdentity();
// builder.Services.UseOAuth2(options =>
// {
//     options.ClientId = "ElsaStudio";
//     options.TokenEndpoint = "https://localhost:44366/connect/token";
//     options.Scope = "YourSite offline_access";
// });
builder.Services.UseOpenIdConnect(openid => configuration.GetSection("Authentication:OpenIdConnect").Bind(openid));
builder.Services.AddDashboardModule();
builder.Services.AddWorkflowsModule();
builder.Services.AddLocalizationModule(localizationConfig);
builder.Services.AddTranslations();

// Replace some services with other implementations.
builder.Services.AddScoped<ITimeZoneProvider, LocalTimeZoneProvider>();
builder.Services.AddScoped<IActivityPickerComponentProvider, TreeviewActivityPickerComponentProvider>();
// Uncomment for the Accordion Activity Picker
//builder.Services.AddScoped<IActivityPickerComponentProvider>(sp => new AccordionActivityPickerComponentProvider
//{
//    // Example - Replace the default category resolver with a custom one.
//    CategoryDisplayResolver = category => category.Split('/').Last().Trim()
//});

// Uncomment for V1 designer theme (default is V2).
// builder.Services.Configure<DesignerOptions>(options =>
// {
//     options.DesignerCssClass = "elsa-flowchart-diagram-designer-v1";
//     options.GraphSettings.Grid.Type = "mesh";
// });

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
app.Run();