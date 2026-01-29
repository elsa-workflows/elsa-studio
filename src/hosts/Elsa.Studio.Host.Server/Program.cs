using Elsa.Studio.Authentication.ElsaIdentity.BlazorServer.Extensions;
using Elsa.Studio.Authentication.ElsaIdentity.HttpMessageHandlers;
using Elsa.Studio.Authentication.ElsaIdentity.UI.Extensions;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Extensions;
using Elsa.Studio.Authentication.OpenIdConnect.HttpMessageHandlers;
using Elsa.Studio.Branding;
using Elsa.Studio.Contracts;
using Elsa.Studio.Core.BlazorServer.Extensions;
using Elsa.Studio.Dashboard.Extensions;
using Elsa.Studio.Extensions;
using Elsa.Studio.Host.Server;
using Elsa.Studio.Localization.BlazorServer.Extensions;
using Elsa.Studio.Localization.Models;
using Elsa.Studio.Localization.Options;
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
using Polly;
using Polly.Extensions.Http;

// Build the host.
var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var environment = builder.Environment; // Add this for early access to environment

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
    
    // Enhanced circuit options for better performance and stability
    options.DetailedErrors = environment.IsDevelopment();
    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
    options.DisconnectedCircuitMaxRetained = 100;
    options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(1);
    
    // Optimize for better performance during feature initialization
    options.MaxBufferedUnacknowledgedRenderBatches = 10;
});

// Choose authentication provider.
// Supported values: "OpenIdConnect" or "ElsaIdentity" (default).
var authProvider = configuration["Authentication:Provider"];
if (string.IsNullOrWhiteSpace(authProvider))
    authProvider = "ElsaIdentity";

Type authenticationHandler;

if (authProvider.Equals("ElsaIdentity", StringComparison.OrdinalIgnoreCase))
{
    // Elsa Identity (username/password against Elsa backend) + login UI at /login.
    builder.Services.AddElsaIdentity();
    builder.Services.AddElsaIdentityUI();
    authenticationHandler = typeof(ElsaIdentityAuthenticatingApiHttpMessageHandler);
}
else if (authProvider.Equals("OpenIdConnect", StringComparison.OrdinalIgnoreCase))
{
    // OpenID Connect.
    builder.Services.AddOpenIdConnectAuth(options =>
    {
        configuration.GetSection("Authentication:OpenIdConnect").Bind(options);

        // If you see a 401 from the OIDC handler while calling the "userinfo" endpoint,
        // either disable UserInfo retrieval (recommended for most setups), or configure your IdP/app registration
        // to allow calling userinfo with the issued access token.
        // options.GetClaimsFromUserInfoEndpoint = false;
    });
    authenticationHandler = typeof(OidcAuthenticatingApiHttpMessageHandler);
}
else if (authProvider.Equals("ElsaLogin", StringComparison.OrdinalIgnoreCase))
{
    // Legacy Elsa Login (username/password against Elsa backend) + login UI at /login.
    builder.Services.AddLoginModule().UseElsaIdentity();
    authenticationHandler = typeof(AuthenticatingApiHttpMessageHandler);
}
else
{
    throw new InvalidOperationException($"Unsupported Authentication:Provider value '{authProvider}'. Supported values are 'OpenIdConnect' and 'ElsaIdentity'.");
}

// Register shell services and modules with enhanced performance configuration.
var backendApiConfig = new BackendApiConfig
{
    ConfigureBackendOptions = options => configuration.GetSection("Backend").Bind(options),
    ConfigureHttpClientBuilder = options =>
    {
        options.AuthenticationHandler = authenticationHandler;
        options.ConfigureHttpClient = (serviceProvider, client) =>
        {
            // Production: Use reasonable timeout for better performance
            // Development: Use longer timeout for debugging
            client.Timeout = environment.IsDevelopment() 
                ? TimeSpan.FromHours(1) 
                : TimeSpan.FromSeconds(30);
                
            // Add performance headers
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
            client.DefaultRequestHeaders.Add("Connection", "keep-alive");
        };
        
        // Add resilience policies for production
        options.ConfigureHttpClientBuilder = clientBuilder =>
        {
            if (!environment.IsDevelopment())
            {
                // Add retry policy for transient failures
                clientBuilder.AddPolicyHandler(HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(
                        retryCount: 2,
                        sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt) * 100)));
            }
        };
    },
};

// Add performance services
builder.Services.AddMemoryCache();

var localizationConfig = new LocalizationConfig
{
    ConfigureLocalizationOptions = options =>
    {
        configuration.GetSection(LocalizationOptions.LocalizationSection).Bind(options);
        options.SupportedCultures = new[] { options.DefaultCulture }
            .Concat(options.SupportedCultures.Where(culture => culture != options.DefaultCulture) ?? []).ToArray();
    }
};

builder.Services.AddScoped<IBrandingProvider, StudioBrandingProvider>();
builder.Services.AddCore().Replace(new(typeof(IBrandingProvider), typeof(StudioBrandingProvider), ServiceLifetime.Scoped));
builder.Services.AddShell(options => configuration.GetSection("Shell").Bind(options));
builder.Services.AddRemoteBackend(backendApiConfig);

builder.Services.AddDashboardModule();
builder.Services.AddWorkflowsModule();
builder.Services.AddLocalizationModule(localizationConfig);
builder.Services.AddTranslations();

// Replace some services with other implementations.
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

// Configure SignalR with enhanced performance and stability settings.
builder.Services.AddSignalR(options =>
{
    // Set MaximumReceiveMessageSize:
    options.MaximumReceiveMessageSize = 5 * 1024 * 1000; // 5MB
    
    // Enhanced connection stability for better post-login performance
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.StreamBufferCapacity = 10;
    
    // Enable detailed errors in development for debugging
    options.EnableDetailedErrors = environment.IsDevelopment();
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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.Run();