using Elsa.Dashboard.Counter.Extensions;
using Elsa.Dashboard.Dashboard.Extensions;
using Elsa.Dashboard.Designer.Components;
using Elsa.Dashboard.Extensions;
using Elsa.Dashboard.Workflows.Extensions;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor(options =>
{
    options.RootComponents.RegisterCustomElement<BlazorButton>("blazor-button");
    options.RootComponents.RegisterCustomElement<BlazorActivity>("blazor-activity");
});
builder.Services.AddMudServices();
builder.Services.AddDashboardServices();
builder.Services.AddDashboardModule();
builder.Services.AddWorkflowsModule();
builder.Services.AddCounterModule();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();