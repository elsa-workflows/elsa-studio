using System.Net;
using Elsa.Api.Client.Resources.ActivityExecutions.Contracts;
using Elsa.Api.Client.Resources.WorkflowInstances.Contracts;
using Elsa.Studio.Authentication.Abstractions.Contracts;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace Elsa.Studio.Workflows.Services;

/// <inheritdoc />
public class WorkflowInstanceObserverFactory(
    IBackendApiClientProvider backendApiClientProvider,
    IRemoteFeatureProvider remoteFeatureProvider,
    ILogger<WorkflowInstanceObserverFactory> logger,
    IHttpConnectionOptionsConfigurator httpConnectionOptionsConfigurator) : IWorkflowInstanceObserverFactory
{
    /// <inheritdoc />
    public Task<IWorkflowInstanceObserver> CreateAsync(string workflowInstanceId)
    {
        var context = new WorkflowInstanceObserverContext
        {
            WorkflowInstanceId = workflowInstanceId
        };

        return CreateAsync(context);
    }

    /// <inheritdoc />
    public async Task<IWorkflowInstanceObserver> CreateAsync(WorkflowInstanceObserverContext context)
    {
        var cancellationToken = context.CancellationToken;
        var workflowInstancesApi = await backendApiClientProvider.GetApiAsync<IWorkflowInstancesApi>(cancellationToken);
        var activityExecutionsApi = await backendApiClientProvider.GetApiAsync<IActivityExecutionsApi>(cancellationToken);
        var workflowInstanceId = context.WorkflowInstanceId;

        // Only establish a SignalR connection if that feature is enabled.
        if (!await remoteFeatureProvider.IsEnabledAsync("Elsa.RealTimeWorkflowUpdates", cancellationToken))
        {
            // Fall back to regular polling.
            return new PollingWorkflowInstanceObserver(
                context,
                workflowInstancesApi,
                activityExecutionsApi);
        }

        // Set up the SignalR connection.
        var baseUrl = backendApiClientProvider.Url;
        var hubUrl = new Uri(baseUrl, "hubs/workflow-instance").ToString();
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                // Configure authentication via the provider-specific configurator
                // Note: Using GetAwaiter().GetResult() here is necessary because WithUrl expects a synchronous Action<HttpConnectionOptions>.
                // This is safe in this context as we're in the configuration phase before the connection starts.
                httpConnectionOptionsConfigurator.ConfigureAsync(options, cancellationToken).GetAwaiter().GetResult();
            })
            .Build();

        var observer = new SignalRWorkflowInstanceObserver(connection);

        try
        {
            await connection.StartAsync(cancellationToken);
        }
        catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogWarning("The workflow instance observer hub was not found, but the RealTimeWorkflows feature was enabled. Please make sure to call `app.UseWorkflowsSignalRHubs()` from the workflow server to install the required SignalR middleware component. Falling back to polling observer");
            return new PollingWorkflowInstanceObserver(
                context,
                workflowInstancesApi,
                activityExecutionsApi);
        }

        await connection.SendAsync("ObserveInstanceAsync", workflowInstanceId, cancellationToken: cancellationToken);
        return observer;
    }
}