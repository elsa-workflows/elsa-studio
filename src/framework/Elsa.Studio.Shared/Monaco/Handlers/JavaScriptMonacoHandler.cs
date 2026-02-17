using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.Services;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Elsa.Studio.Monaco.Handlers;

/// <summary>
/// Handles Monaco editor for JavaScript.
/// </summary>
public class JavaScriptMonacoHandler(IJSRuntime jsRuntime, TypeDefinitionService typeDefinitionService, ILogger<JavaScriptMonacoHandler> logger)
    : IMonacoHandler
{
    private const int MaxRetries = 3;
    private const int InitialDelayMs = 100;

    /// <inheritdoc />
    public async ValueTask InitializeAsync(MonacoContext context)
    {
        if (context.ExpressionDescriptor.Type != "JavaScript")
            return;

        var activityDescriptor = context.CustomProperties.TryGetValue(nameof(ActivityDescriptor), out var activityDescriptorObj) ? (ActivityDescriptor)activityDescriptorObj : null;
        var propertyDescriptor = context.CustomProperties.TryGetValue(nameof(PropertyDescriptor), out var propertyDescriptorObj) ? (PropertyDescriptor)propertyDescriptorObj : null;
        var workflowDefinitionId = context.CustomProperties.TryGetValue("WorkflowDefinitionId", out var workflowDefinitionIdObj) ? (string)workflowDefinitionIdObj : null;
        var activityTypeName = activityDescriptor?.TypeName;
        var propertyName = propertyDescriptor?.Name;

        if (activityTypeName == null || workflowDefinitionId == null)
            return;

        var data = await typeDefinitionService.GetTypeDefinition(workflowDefinitionId, activityTypeName, propertyName ?? string.Empty);
        
        // Add retry logic with exponential backoff to handle race conditions with Monaco editor initialization
        await InvokeWithRetryAsync(async () =>
        {
            await jsRuntime.InvokeVoidAsync("monaco.languages.typescript.javascriptDefaults.addExtraLib", data, null);
        });
    }

    private async Task InvokeWithRetryAsync(Func<Task> action)
    {
        var delay = InitialDelayMs;
        
        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                await action();
                return;
            }
            catch (JSException ex) when (attempt < MaxRetries)
            {
                logger.LogWarning(ex, "Failed to initialize Monaco editor on attempt {Attempt} of {MaxRetries}. Retrying after {Delay}ms...", attempt, MaxRetries, delay);
                await Task.Delay(delay);
                delay *= 2; // Exponential backoff
            }
            catch (JSException ex)
            {
                // Log the final failure but don't throw - this is a non-critical enhancement feature
                logger.LogWarning(ex, "Failed to initialize Monaco editor after {MaxRetries} attempts. Type definitions will not be available for IntelliSense.", MaxRetries);
            }
        }
    }
}