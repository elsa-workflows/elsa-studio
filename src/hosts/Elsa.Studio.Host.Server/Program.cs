using Blazored.LocalStorage;
using Elsa.Studio.Backend.Extensions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Counter.Extensions;
using Elsa.Studio.Dashboard.Extensions;
using Elsa.Studio.Host.Server.HostedServices;
using Elsa.Studio.Shell.Extensions;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Environments.Extensions;
using Elsa.Studio.Host.Server.Services;
using Elsa.Studio.Login.Extensions;
using Elsa.Studio.Secrets.Extensions;
using Elsa.Studio.Security.Extensions;
using Elsa.Studio.Services;
using Elsa.Studio.Webhooks.Extensions;
using Elsa.Studio.Workflows.Designer.Extensions;
using Microsoft.AspNetCore.Components.Authorization;

// Build the host.
var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Register the services.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor(options =>
{
    // Register the root components.
    options.RootComponents.RegisterCustomElements();
});

// Add the shell and module services.
builder.Services.AddShell();
builder.Services.AddBackendModule(options => configuration.GetSection("Backend").Bind(options));
builder.Services.AddLoginModule();
builder.Services.AddEnvironmentsModule();
builder.Services.AddDashboardModule();
builder.Services.AddWorkflowsModule();
builder.Services.AddSecurityModule();
builder.Services.AddSecretsModule();
builder.Services.AddWebhooksModule();
builder.Services.AddCounterModule();

// Blazored.
builder.Services.AddBlazoredLocalStorage();

// Register authorization.
builder.Services.AddOptions();
builder.Services.AddAuthorizationCore();
builder.Services.AddSingleton<IJwtParser, ServerJwtParser>();
builder.Services.AddScoped<IJwtAccessor, ServerJwtAccessor>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuthenticationStateProvider, DefaultAuthenticationStateProvider>();

// Register the hosted services.
builder.Services.AddHostedService<RunStartupTasksHostedService>();

// Configure SignalR.
builder.Services.AddSignalR(options =>
{
    // Set MaximumReceiveMessageSize to 512kb:
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

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// Run the application.
app.Run();