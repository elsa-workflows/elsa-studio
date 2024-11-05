using System.Reflection;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Services;

/// <summary>
/// Decorates an API client with a Blazor service accessor, ensuring that the service provider is available to the API client when calling DI-resolved delegating handlers.
/// </summary>
public class BlazorScopedProxyApi<T> : DispatchProxy
{
    private T _decoratedApi = default!;
    private IBlazorServiceAccessor _blazorServiceAccessor = default!;
    private IServiceProvider _serviceProvider = default!;

    internal void Initialize(T decoratedApi, IBlazorServiceAccessor blazorServiceAccessor, IServiceProvider serviceProvider)
    {
        _decoratedApi = decoratedApi;
        _blazorServiceAccessor = blazorServiceAccessor;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        _blazorServiceAccessor.Services = _serviceProvider;
        return targetMethod?.Invoke(_decoratedApi, args);
    }
}