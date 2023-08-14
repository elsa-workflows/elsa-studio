using Blazored.LocalStorage;
using Elsa.Studio.Backend.Extensions;
using Elsa.Studio.Dashboard.Extensions;
using Elsa.Studio.Shell.Extensions;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Environments.Extensions;
using Elsa.Studio.Host.CustomElements.Components;
using Elsa.Studio.Host.CustomElements.Services;
using Elsa.Studio.Login.Extensions;
using Elsa.Studio.Secrets.Extensions;
using Elsa.Studio.Security.Extensions;
using Elsa.Studio.Services;
using Elsa.Studio.Webhooks.Extensions;
using Elsa.Studio.Workflows.Designer.Extensions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

// Build the host.
var builder = WebAssemblyHostBuilder.CreateDefault(args);
var configuration = builder.Configuration;

// Register the custom elements.
builder.RootComponents.RegisterCustomElements();
builder.RootComponents.RegisterCustomElement<WorkflowDefinitionEditor>("elsa-studio-workflow-definition-editor");

// Register the modules.
builder.Services.AddShell();
builder.Services.AddBackendModule(options => configuration.GetSection("Backend").Bind(options));
builder.Services.AddLoginModule();
builder.Services.AddEnvironmentsModule();
builder.Services.AddDashboardModule();
builder.Services.AddWorkflowsModule();
builder.Services.AddSecurityModule();
builder.Services.AddSecretsModule();
builder.Services.AddWebhooksModule();

// Blazored.
builder.Services.AddBlazoredLocalStorage();

// Register authorization.
builder.Services.AddOptions();
builder.Services.AddAuthorizationCore();
builder.Services.AddSingleton<IJwtParser, ClientJwtParser>();
builder.Services.AddScoped<IJwtAccessor, ClientJwtAccessor>();
builder.Services.AddScoped<AuthenticationStateProvider, DefaultAuthenticationStateProvider>();

// Build the application.
var app = builder.Build();

// Run each startup task.
var startupTask = app.Services.GetServices<IStartupTask>();
foreach (var task in startupTask) await task.ExecuteAsync();

// Run the application.
await app.RunAsync();