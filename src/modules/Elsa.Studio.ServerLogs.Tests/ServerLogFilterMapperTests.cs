using Elsa.Studio.ServerLogs.Models;
using Elsa.Studio.ServerLogs.Services;
using Xunit;

namespace Elsa.Studio.ServerLogs.Tests;

public class ServerLogFilterMapperTests
{
    [Fact]
    public void ToRecentRequest_MapsFilterFields()
    {
        var from = DateTimeOffset.UtcNow.AddMinutes(-10);
        var to = DateTimeOffset.UtcNow;
        var filter = new ServerLogFilter
        {
            MinimumLevel = ServerLogLevel.Warning,
            Levels = [ServerLogLevel.Error, ServerLogLevel.Critical],
            CategoryPrefix = "Elsa.Workflows",
            Text = "incident",
            TenantId = "tenant-a",
            WorkflowDefinitionId = "definition-a",
            WorkflowInstanceId = "instance-a",
            TraceId = "trace-a",
            CorrelationId = "correlation-a",
            SourceId = "pod-a",
            From = from,
            To = to,
            Take = 50
        };

        var request = ServerLogFilterMapper.ToRecentRequest(filter, 100);

        Assert.Equal(filter.MinimumLevel, request.MinimumLevel);
        Assert.Equal(filter.Levels, request.Levels);
        Assert.Equal(filter.CategoryPrefix, request.CategoryPrefix);
        Assert.Equal(filter.Text, request.Text);
        Assert.Equal(filter.TenantId, request.TenantId);
        Assert.Equal(filter.WorkflowDefinitionId, request.WorkflowDefinitionId);
        Assert.Equal(filter.WorkflowInstanceId, request.WorkflowInstanceId);
        Assert.Equal(filter.TraceId, request.TraceId);
        Assert.Equal(filter.CorrelationId, request.CorrelationId);
        Assert.Equal(filter.SourceId, request.SourceId);
        Assert.Equal(from, request.From);
        Assert.Equal(to, request.To);
        Assert.Equal(50, request.Take);
    }

    [Fact]
    public void ToRecentRequest_ClampsTakeToRowCap()
    {
        var request = ServerLogFilterMapper.ToRecentRequest(new() { Take = 500 }, 100);

        Assert.Equal(100, request.Take);
    }

    [Fact]
    public void ToRecentRequest_DefaultsTakeToRowCap()
    {
        var request = ServerLogFilterMapper.ToRecentRequest(new(), 100);

        Assert.Equal(100, request.Take);
    }

    [Fact]
    public void ToLiveSubscription_PreservesSourceFilter()
    {
        var request = ServerLogFilterMapper.ToLiveSubscription(new() { SourceId = "pod-a" });

        Assert.Equal("pod-a", request.SourceId);
    }

    [Fact]
    public void ToLiveSubscription_CopiesLevelCollection()
    {
        var filter = new ServerLogFilter { Levels = [ServerLogLevel.Warning] };
        var request = ServerLogFilterMapper.ToLiveSubscription(filter);

        filter.Levels.Add(ServerLogLevel.Error);

        Assert.Equal([ServerLogLevel.Warning], request.Levels);
    }
}
