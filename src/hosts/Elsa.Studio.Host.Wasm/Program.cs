using Elsa.Studio.Backend.Extensions;
using Elsa.Studio.Dashboard.Extensions;
using Elsa.Studio.Shell;
using Elsa.Studio.Shell.Extensions;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Counter.Extensions;
using Elsa.Studio.Designer.Extensions;
using Elsa.Studio.Environments.Extensions;
using Elsa.Studio.Login.Extensions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

// Build the host.
var builder = WebAssemblyHostBuilder.CreateDefault(args);
var configuration = builder.Configuration;

// Register the root components.
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.RootComponents.RegisterCustomElements();

// Register the services.
builder.Services.AddShell();
builder.Services.AddBackend(options => configuration.GetSection("Backend").Bind(options));
builder.Services.AddLogin();
builder.Services.AddEnvironments();
builder.Services.AddDashboardModule();
builder.Services.AddWorkflowsModule();
builder.Services.AddCounterModule();

// Register authorization.
builder.Services.AddOptions();
builder.Services.AddAuthorizationCore();

// Build the application.
var app = builder.Build();

// Run each startup task.
var startupTask = app.Services.GetServices<IStartupTask>();
foreach (var task in startupTask) await task.ExecuteAsync();

// Run the application.
await app.RunAsync();