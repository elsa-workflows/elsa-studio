using System.Net;
using Elsa.Api.Client.Resources.WorkflowInstances.Contracts;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Extensions;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace Elsa.Studio.Workflows.Services;

/// <inheritdoc />
public class WorkflowInstanceObserverFactory(
    IRemoteBackendApiClientProvider remoteBackendApiClientProvider,
    IRemoteFeatureProvider remoteFeatureProvider,
    IHttpMessageHandlerFactory httpMessageHandlerFactory,
    IBlazorServiceAccessor blazorServiceAccessor,
    IServiceProvider serviceProvider,
    ILoggerFactory loggerFactory,
    ILogger<WorkflowInstanceObserverFactory> logger) : IWorkflowInstanceObserverFactory
{
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(1);
    
    /// <inheritdoc />
    public async Task<IWorkflowInstanceObserver> CreateAsync(string workflowInstanceId, string name, CancellationToken cancellationToken = default)
    {
        var api = await remoteBackendApiClientProvider.GetApiAsync<IWorkflowInstancesApi>(cancellationToken);
        var pollingLogger = loggerFactory.CreateLogger<PollingWorkflowInstanceObserver>();

        // Only observe the workflow instance if the feature is enabled.
        if (!await remoteFeatureProvider.IsEnabledAsync("Elsa.RealTimeWorkflowUpdates", cancellationToken))
        {
            return new PollingWorkflowInstanceObserver(blazorServiceAccessor, serviceProvider, api, workflowInstanceId, _pollingInterval, pollingLogger)
            {
                Name = name
            };
        }

        // Get the SignalR connection.
        var baseUrl = remoteBackendApiClientProvider.Url;
        var hubUrl = new Uri(baseUrl, "hubs/workflow-instance").ToString();
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, httpMessageHandlerFactory)
            .Build();

        var observer = new SignalRWorkflowInstanceObserver(connection)
        {
            Name = name
        };

        try
        {
            await connection.StartAsync(cancellationToken);
        }
        catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogWarning("The workflow instance observer hub was not found, but the RealTimeWorkflows feature was enabled. Please make sure to call `app.UseWorkflowsSignalRHubs()` from the workflow server to install the required SignalR middleware component. Falling back to disconnected observer");
            return new PollingWorkflowInstanceObserver(blazorServiceAccessor, serviceProvider, api, workflowInstanceId, _pollingInterval, pollingLogger)
            {
                Name = name
            };
        }

        await connection.SendAsync("ObserveInstanceAsync", workflowInstanceId, cancellationToken: cancellationToken);

        return observer;
    }
}