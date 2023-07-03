using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Api.Client.Activities;
using Elsa.Api.Client.Converters;
using Elsa.Studio.Workflows.Designer.Contracts;
using Elsa.Studio.Workflows.Designer.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Elsa.Studio.Workflows.Designer.Interop;

public class X6GraphApi
{
    private readonly IJSObjectReference _module;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _containerId;

    public X6GraphApi(IJSObjectReference module, IServiceProvider serviceProvider, string containerId)
    {
        _module = module;
        _serviceProvider = serviceProvider;
        _containerId = containerId;
    }
    
    public async Task<JsonElement> ReadGraphAsync() => await InvokeAsync(module => module.InvokeAsync<JsonElement>("readGraph", _containerId));
    public async Task DisposeGraphAsync() => await TryInvokeAsync(module => module.InvokeVoidAsync("disposeGraph", _containerId));
    public async Task SetGridColorAsync(string color) => await InvokeAsync(module => module.InvokeVoidAsync("setGridColor", _containerId, color));
    
    public async Task AddActivityNodeAsync(X6Node node)
    {
        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        serializerOptions.Converters.Add(new ActivityJsonConverterFactory(_serviceProvider));
        serializerOptions.Converters.Add(new ExpressionJsonConverterFactory());
        serializerOptions.Converters.Add(new JsonStringEnumConverter());
        
        var nodeElement = JsonSerializer.SerializeToElement(node, serializerOptions);
        
        await InvokeAsync(module => module.InvokeVoidAsync("addActivityNode", _containerId, nodeElement));
    }

    public async Task LoadGraphAsync(X6Graph graph) => await InvokeAsync(module => module.InvokeVoidAsync("loadGraph", _containerId, graph));
    public async Task ZoomToFitAsync() => await InvokeAsync(module => module.InvokeVoidAsync("zoomToFit", _containerId));
    public async Task CenterContentAsync() => await InvokeAsync(module => module.InvokeVoidAsync("centerContent", _containerId));
    
    public async Task UpdateActivityAsync(string id, Activity activity)
    {
        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        serializerOptions.Converters.Add(new ActivityJsonConverterFactory(_serviceProvider));
        serializerOptions.Converters.Add(new ExpressionJsonConverterFactory());
        serializerOptions.Converters.Add(new JsonStringEnumConverter());

        var mapperFactory = _serviceProvider.GetRequiredService<IMapperFactory>();
        var activityMapper = await mapperFactory.CreateActivityMapperAsync();
        var ports = activityMapper.GetPorts(activity);
        var activityElement = JsonSerializer.SerializeToElement(activity, serializerOptions);
        await InvokeAsync(module => module.InvokeVoidAsync("updateActivity", _containerId, id, activityElement, ports));
    }

    private async Task InvokeAsync(Func<IJSObjectReference, ValueTask> func) => await func(_module);
    
    private async Task TryInvokeAsync(Func<IJSObjectReference, ValueTask> func)
    {
        try
        {
            await func(_module);
        }
        catch (JSDisconnectedException)
        {
            // Ignore.
        }
    }

    private async Task<T> InvokeAsync<T>(Func<IJSObjectReference, ValueTask<T>> func) => await func(_module);
}