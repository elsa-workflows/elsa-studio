using Elsa.Studio.Shell.Extensions;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Core.BlazorWasm.Extensions;
using Elsa.Studio.Extensions;
using Elsa.Studio.Host.CustomElements.Components;
using Elsa.Studio.Host.CustomElements.Extensions;
using Elsa.Studio.Host.CustomElements.HttpMessageHandlers;
using Elsa.Studio.Host.CustomElements.Services;
using Elsa.Studio.Localization.Time;
using Elsa.Studio.Localization.Time.Providers;
using Elsa.Studio.Localization.Models;
using Elsa.Studio.Localization.BlazorWasm.Extensions;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Designer.Extensions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;

// Build the host.
var builder = WebAssemblyHostBuilder.CreateDefault(args);
var configuration = builder.Configuration;

// Get shadow DOM configuration
var enableShadowDOM = configuration.GetValue<bool>("ShadowDOM:Enabled", false);

// Register the custom elements.
builder.RootComponents.RegisterCustomElsaStudioElements();

if (enableShadowDOM)
{
    // Register custom elements with Shadow DOM support
    builder.RootComponents.RegisterCustomElementWithShadowDOM<BackendProvider>("elsa-backend-provider-shadow");
    builder.RootComponents.RegisterCustomElementWithShadowDOM<WorkflowDefinitionEditorWrapper>("elsa-workflow-definition-editor-shadow");
    builder.RootComponents.RegisterCustomElementWithShadowDOM<WorkflowInstanceViewerWrapper>("elsa-workflow-instance-viewer-shadow");
    builder.RootComponents.RegisterCustomElementWithShadowDOM<WorkflowInstanceListWrapper>("elsa-workflow-instance-list-shadow");
    builder.RootComponents.RegisterCustomElementWithShadowDOM<WorkflowDefinitionListWrapper>("elsa-workflow-definition-list-shadow");
}
else
{
    // Register custom elements without Shadow DOM (original behavior)
    builder.RootComponents.RegisterCustomElement<BackendProvider>("elsa-backend-provider");
    builder.RootComponents.RegisterCustomElement<WorkflowDefinitionEditorWrapper>("elsa-workflow-definition-editor");
    builder.RootComponents.RegisterCustomElement<WorkflowInstanceViewerWrapper>("elsa-workflow-instance-viewer");
    builder.RootComponents.RegisterCustomElement<WorkflowInstanceListWrapper>("elsa-workflow-instance-list");
    builder.RootComponents.RegisterCustomElement<WorkflowDefinitionListWrapper>("elsa-workflow-definition-list");
}

// Register local services.
builder.Services.AddSingleton<BackendService>();

// Register the modules.
var backendApiConfig = new BackendApiConfig
{
    ConfigureBackendOptions = options => configuration.GetSection("Backend").Bind(options),
    ConfigureHttpClientBuilder = options =>
    {
        options.ApiKey = configuration["Backend:ApiKey"];
        options.AuthenticationHandler = typeof(AuthHttpMessageHandler);
    }, 
};

var localizationConfig = new LocalizationConfig
{
    ConfigureLocalizationOptions = options => configuration.GetSection("Localization").Bind(options),
};

builder.Services.AddCore();
builder.Services.AddShell();
builder.Services.AddRemoteBackend(backendApiConfig);
builder.Services.Replace(ServiceDescriptor.Scoped<IRemoteBackendAccessor, ComponentRemoteBackendAccessor>());
builder.Services.AddWorkflowsModule();
builder.Services.AddLocalizationModule(localizationConfig);
builder.Services.AddScoped<ITimeZoneProvider, LocalTimeZoneProvider>();

// Build the application.
var app = builder.Build();

await app.UseElsaLocalization();

// Run each startup task.
var startupTask = app.Services.GetServices<IStartupTask>();
foreach (var task in startupTask) await task.ExecuteAsync();

// Run the application.
await app.RunAsync();

