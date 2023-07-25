using Elsa.Studio.Backend.Contracts;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Microsoft.AspNetCore.SignalR.Client;

namespace Elsa.Studio.Workflows.Services;

public class WorkflowInstanceObserverFactory : IWorkflowInstanceObserverFactory
{
    private readonly IBackendConnectionProvider _backendConnectionProvider;
    private readonly IFeatureService _featureService;

    public WorkflowInstanceObserverFactory(IBackendConnectionProvider backendConnectionProvider, IFeatureService featureService)
    {
        _backendConnectionProvider = backendConnectionProvider;
        _featureService = featureService;
    }
    
    public async Task<IWorkflowInstanceObserver> CreateAsync(string workflowInstanceId, CancellationToken cancellationToken = default)
    {
        // Only observe the workflow instance if the feature is enabled.
        if (!await _featureService.IsEnabledAsync("Elsa.RealTimeWorkflowUpdates", cancellationToken))
            return new DisconnectedWorkflowInstanceObserver();

        // Get the SignalR connection.
        var baseUrl = _backendConnectionProvider.Url;
        var hubUrl = new Uri(baseUrl, "hubs/workflow-instance").ToString();
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .Build();

        var observer = new WorkflowInstanceObserver(connection);
        await connection.StartAsync(cancellationToken);
        await connection.SendAsync("ObserveInstanceAsync", workflowInstanceId, cancellationToken: cancellationToken);

        return observer;
    }
}