using Elsa.Studio.Dashboard.Models;

namespace Elsa.Studio.Dashboard.Services;

public record DashboardRangeOption(string Value, string Label);

public static class DashboardRangeMapper
{
    public static IReadOnlyList<DashboardRangeOption> Options { get; } =
    [
        new(DashboardRangeKeys.OneHour, "1h"),
        new(DashboardRangeKeys.TwentyFourHours, "24h"),
        new(DashboardRangeKeys.SevenDays, "7d")
    ];

    public static string Normalize(string? range)
    {
        return Options.Any(x => x.Value == range) ? range! : DashboardRangeKeys.TwentyFourHours;
    }

    public static string GetDefaultGranularity(string? range)
    {
        return Normalize(range) switch
        {
            DashboardRangeKeys.OneHour => DashboardTrendGranularity.Minute,
            DashboardRangeKeys.SevenDays => DashboardTrendGranularity.Day,
            _ => DashboardTrendGranularity.Hour
        };
    }
}
