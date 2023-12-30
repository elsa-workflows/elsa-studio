using Elsa.Studio.Contracts;
using Elsa.Studio.Services;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Components;

/// Base class for components. This class sets the <see cref="BlazorServiceAccessor.Services"/> property to the <see cref="IServiceProvider"/> instance.
/// See https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/dependency-injection?view=aspnetcore-7.0#access-server-side-blazor-services-from-a-different-di-scope
public abstract class StudioComponentBase : ComponentBase, IHandleEvent, IHandleAfterRender
{
    private bool _hasCalledOnAfterRender;

    /// Gets the current IServiceProvider.
    [Inject] protected IServiceProvider Services { get; set; } = default!;

    /// Gets the current <see cref="IBlazorServiceAccessor"/>.
    [Inject] protected IBlazorServiceAccessor BlazorServiceAccessor { get; set; } = default!;

    /// <inheritdoc />
    public override Task SetParametersAsync(ParameterView parameters) => InvokeWithBlazorServiceContext(() => base.SetParametersAsync(parameters));

    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg)
    {
        return InvokeWithBlazorServiceContext(() =>
        {
            var task = callback.InvokeAsync(arg);
            var shouldAwaitTask = task.Status != TaskStatus.RanToCompletion && task.Status != TaskStatus.Canceled;

            StateHasChanged();

            return shouldAwaitTask ? CallStateHasChangedOnAsyncCompletion(task) : Task.CompletedTask;
        });
    }

    Task IHandleAfterRender.OnAfterRenderAsync()
    {
        return InvokeWithBlazorServiceContext(() =>
        {
            var firstRender = !_hasCalledOnAfterRender;
            _hasCalledOnAfterRender = true;

            OnAfterRender(firstRender);

            return OnAfterRenderAsync(firstRender);
        });
    }

    private async Task CallStateHasChangedOnAsyncCompletion(Task task)
    {
        try
        {
            await task;
        }
        catch
        {
            if (task.IsCanceled)
            {
                return;
            }

            throw;
        }

        StateHasChanged();
    }

    /// <summary>
    /// Invokes the given function with the Blazor service context.
    /// </summary>
    /// <param name="func">The function to be invoked.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected async Task InvokeWithBlazorServiceContext(Func<Task> func)
    {
        try
        {
            BlazorServiceAccessor.Services = Services;
            await func();
        }
        finally
        {
            BlazorServiceAccessor.Services = null!;
        }
    }
    
    /// <summary>
    /// Invokes the given function with the Blazor service context.
    /// </summary>
    /// <param name="func">The function to be invoked.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected async Task<T> InvokeWithBlazorServiceContext<T>(Func<Task<T>> func)
    {
        try
        {
            BlazorServiceAccessor.Services = Services;
            return await func();
        }
        finally
        {
            BlazorServiceAccessor.Services = null!;
        }
    }
}