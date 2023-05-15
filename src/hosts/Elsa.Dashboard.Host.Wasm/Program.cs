using Elsa.Dashboard.Contracts;
using Elsa.Dashboard.Counter.Extensions;
using Elsa.Dashboard.Dashboard.Extensions;
using Elsa.Dashboard.Designer.Extensions;
using Elsa.Dashboard.Environments.Extensions;
using Elsa.Dashboard.Shell;
using Elsa.Dashboard.Shell.Extensions;
using Elsa.Dashboard.Workflows.Extensions;
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