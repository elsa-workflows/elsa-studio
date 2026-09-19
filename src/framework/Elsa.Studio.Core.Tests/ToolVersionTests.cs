using Xunit;

namespace Elsa.Studio.Core.Tests;

public class ToolVersionTests
{
    [Fact]
    public void GetDisplayVersion_ReturnsCurrentMinorVersion()
    {
        Assert.Equal(new Version(3, 9, 0, 0), ToolVersion.Version);
        Assert.Equal("3.9", ToolVersion.GetDisplayVersion());
    }
}
