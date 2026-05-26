using Elsa.Studio.Diagnostics.OpenTelemetry.Models;
using Elsa.Studio.Diagnostics.OpenTelemetry.Services;
using Xunit;

namespace Elsa.Studio.Diagnostics.OpenTelemetry.Tests;

public class OpenTelemetryFilterMapperTests
{
    [Fact]
    public void ToTraceRequest_WhenTakeExceedsRowCap_ClampsTake()
    {
        var request = OpenTelemetryFilterMapper.ToTraceRequest(new OpenTelemetryTraceFilter { Take = 500 }, 100);

        Assert.Equal(100, request.Take);
    }

    [Fact]
    public void ToMetricRequest_WhenTakeIsMissing_UsesRowCap()
    {
        var request = OpenTelemetryFilterMapper.ToMetricRequest(new OpenTelemetryMetricFilter(), 75);

        Assert.Equal(75, request.Take);
    }

    [Fact]
    public void ToLogRequest_WhenRowCapIsInvalid_UsesMinimumTake()
    {
        var request = OpenTelemetryFilterMapper.ToLogRequest(new OpenTelemetryLogFilter(), 0);

        Assert.Equal(1, request.Take);
    }
}
