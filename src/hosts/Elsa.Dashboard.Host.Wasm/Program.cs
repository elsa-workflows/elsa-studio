using Elsa.Dashboard.Counter.Extensions;
using Elsa.Dashboard.Dashboard.Extensions;
using Elsa.Dashboard.Designer.Components;
using Elsa.Dashboard.Extensions;
using Elsa.Dashboard.Shell;
using Elsa.Dashboard.Workflows.Extensions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.RootComponents.RegisterCustomElement<BlazorButton>("blazor-button");
builder.RootComponents.RegisterCustomElement<BlazorActivity>("blazor-activity");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddMudServices();
builder.Services.AddDashboardServices();
builder.Services.AddDashboardModule();
builder.Services.AddWorkflowsModule();
builder.Services.AddCounterModule();

await builder.Build().RunAsync();