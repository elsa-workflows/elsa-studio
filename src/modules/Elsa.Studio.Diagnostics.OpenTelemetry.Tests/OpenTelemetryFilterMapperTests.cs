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
}
