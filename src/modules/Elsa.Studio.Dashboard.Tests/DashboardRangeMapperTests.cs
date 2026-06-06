using Elsa.Studio.Dashboard.Models;
using Elsa.Studio.Dashboard.Services;
using Xunit;

namespace Elsa.Studio.Dashboard.Tests;

public class DashboardRangeMapperTests
{
    [Theory]
    [InlineData("1h", DashboardRangeKeys.OneHour)]
    [InlineData("24h", DashboardRangeKeys.TwentyFourHours)]
    [InlineData("7d", DashboardRangeKeys.SevenDays)]
    [InlineData(null, DashboardRangeKeys.TwentyFourHours)]
    [InlineData("invalid", DashboardRangeKeys.TwentyFourHours)]
    public void Normalize_ReturnsSupportedRangeOrDefault(string? range, string expected)
    {
        var result = DashboardRangeMapper.Normalize(range);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("1h", DashboardTrendGranularity.Minute)]
    [InlineData("24h", DashboardTrendGranularity.Hour)]
    [InlineData("7d", DashboardTrendGranularity.Day)]
    [InlineData("invalid", DashboardTrendGranularity.Hour)]
    public void GetDefaultGranularity_ReturnsRangeGranularity(string range, string expected)
    {
        var result = DashboardRangeMapper.GetDefaultGranularity(range);

        Assert.Equal(expected, result);
    }
}
