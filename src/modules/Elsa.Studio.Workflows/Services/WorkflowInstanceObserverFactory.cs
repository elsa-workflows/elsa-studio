using System.Net;
using Elsa.Api.Client.Resources.ActivityExecutions.Contracts;
using Elsa.Api.Client.Resources.WorkflowInstances.Contracts;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Workflows.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace Elsa.Studio.Workflows.Services;

/// <inheritdoc />
public class WorkflowInstanceObserverFactory(
    IRemoteBackendApiClientProvider remoteBackendApiClientProvider,
    IRemoteFeatureProvider remoteFeatureProvider,
    IHttpMessageHandlerFactory httpMessageHandlerFactory,
    ILogger<WorkflowInstanceObserverFactory> logger) : IWorkflowInstanceObserverFactory
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
        var workflowInstancesApi = await remoteBackendApiClientProvider.GetApiAsync<IWorkflowInstancesApi>(cancellationToken);
        var activityExecutionsApi = await remoteBackendApiClientProvider.GetApiAsync<IActivityExecutionsApi>(cancellationToken);
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

        // Set-up the SignalR connection.
        var baseUrl = remoteBackendApiClientProvider.Url;
        var hubUrl = new Uri(baseUrl, "hubs/workflow-instance").ToString();
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, httpMessageHandlerFactory)
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