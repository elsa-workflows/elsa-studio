using Elsa.Studio.Dashboard.Extensions;
using Elsa.Studio.Shell;
using Elsa.Studio.Shell.Extensions;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Counter.Extensions;
using Elsa.Studio.Designer.Extensions;
using Elsa.Studio.Environments.Extensions;
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
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddShell();
builder.Services.AddEnvironments(options => configuration.GetSection("Environments").Bind(options));
builder.Services.AddDashboardModule();
builder.Services.AddWorkflowsModule();
builder.Services.AddCounterModule();

// Build the application.
var app = builder.Build();

// Run each startup task.
var startupTask = app.Services.GetServices<IStartupTask>();
foreach (var task in startupTask) await task.ExecuteAsync();

// Run the application.
await app.RunAsync();