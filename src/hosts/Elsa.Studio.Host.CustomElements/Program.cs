using Elsa.Studio.Shell.Extensions;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Core.BlazorWasm.Extensions;
using Elsa.Studio.Extensions;
using Elsa.Studio.Host.CustomElements.Components;
using Elsa.Studio.Host.CustomElements.HttpMessageHandlers;
using Elsa.Studio.Host.CustomElements.Services;
using Elsa.Studio.Workflows.Designer.Extensions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;

// Build the host.
var builder = WebAssemblyHostBuilder.CreateDefault(args);
var configuration = builder.Configuration;

// Register the custom elements.
builder.RootComponents.RegisterCustomElsaStudioElements();
builder.RootComponents.RegisterCustomElement<Backend>("elsa-backend");
builder.RootComponents.RegisterCustomElement<WorkflowDefinitionEditorWrapper>("elsa-workflow-definition-editor");

// Register local services.
builder.Services.AddSingleton<BackendService>();

// Register the modules.
builder.Services.AddCore();
builder.Services.AddShell();
builder.Services.AddRemoteBackend(
    elsaClient =>
    {
        elsaClient.ApiKey = configuration["Backend:ApiKey"];
        elsaClient.AuthenticationHandler = typeof(AuthHttpMessageHandler);
    },
    options => configuration.GetSection("Backend").Bind(options));
builder.Services.Replace(ServiceDescriptor.Scoped<IRemoteBackendAccessor, ComponentRemoteBackendAccessor>());
builder.Services.AddWorkflowsModule();

// Build the application.
var app = builder.Build();

// Run each startup task.
var startupTask = app.Services.GetServices<IStartupTask>();
foreach (var task in startupTask) await task.ExecuteAsync();

// Run the application.
await app.RunAsync();

