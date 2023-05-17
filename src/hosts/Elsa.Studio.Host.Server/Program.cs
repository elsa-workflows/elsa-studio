using System.Security.Claims;
using Elsa.Studio.Backend.Extensions;
using Elsa.Studio.Counter.Extensions;
using Elsa.Studio.Dashboard.Extensions;
using Elsa.Studio.Host.Server.HostedServices;
using Elsa.Studio.Shell.Extensions;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Designer.Extensions;
using Elsa.Studio.Environments.Extensions;
using Elsa.Studio.Login.Extensions;
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
builder.Services.AddBackend(options => configuration.GetSection("Backend").Bind(options));
builder.Services.AddLogin();
builder.Services.AddEnvironments();
builder.Services.AddDashboardModule();
builder.Services.AddWorkflowsModule();
builder.Services.AddCounterModule();

// Register authorization.
builder.Services.AddOptions();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, TestAuthStateProvider>();

// Register the hosted services.
builder.Services.AddHostedService<RunStartupTasksHostedService>();

// Build the application.
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
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

/// <summary>
/// Temporary authentication state provider for testing purposes.
/// </summary>
public class TestAuthStateProvider : AuthenticationStateProvider
{
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "John Doe"),
            new(ClaimTypes.Role, "Administrator")
        };
        var anonymous = new ClaimsIdentity(claims, "testAuthType");
        return await Task.FromResult(new AuthenticationState(new ClaimsPrincipal(anonymous)));
    }
}