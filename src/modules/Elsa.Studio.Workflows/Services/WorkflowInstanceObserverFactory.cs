using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Extensions;
using Microsoft.AspNetCore.SignalR.Client;

namespace Elsa.Studio.Workflows.Services;

/// <inheritdoc />
public class WorkflowInstanceObserverFactory : IWorkflowInstanceObserverFactory
{
    private readonly IRemoteBackendApiClientProvider _remoteBackendApiClientProvider;
    private readonly IRemoteFeatureProvider _remoteFeatureProvider;
    private readonly IHttpMessageHandlerFactory _httpMessageHandlerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowInstanceObserverFactory"/> class.
    /// </summary>
    public WorkflowInstanceObserverFactory(IRemoteBackendApiClientProvider remoteBackendApiClientProvider, IRemoteFeatureProvider remoteFeatureProvider, IHttpMessageHandlerFactory httpMessageHandlerFactory)
    {
        _remoteBackendApiClientProvider = remoteBackendApiClientProvider;
        _remoteFeatureProvider = remoteFeatureProvider;
        _httpMessageHandlerFactory = httpMessageHandlerFactory;
    }

    /// <inheritdoc />
    public async Task<IWorkflowInstanceObserver> CreateAsync(string workflowInstanceId, CancellationToken cancellationToken = default)
    {
        // Only observe the workflow instance if the feature is enabled.
        if (!await _remoteFeatureProvider.IsEnabledAsync("Elsa.RealTimeWorkflowUpdates", cancellationToken))
            return new DisconnectedWorkflowInstanceObserver();

        // Get the SignalR connection.
        var baseUrl = _remoteBackendApiClientProvider.Url;
        var hubUrl = new Uri(baseUrl, "hubs/workflow-instance").ToString();
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, _httpMessageHandlerFactory)            
            .Build();

        var observer = new WorkflowInstanceObserver(connection);
        await connection.StartAsync(cancellationToken);
        await connection.SendAsync("ObserveInstanceAsync", workflowInstanceId, cancellationToken: cancellationToken);

        return observer;
    }
}