namespace Elsa.Studio.Secrets;

/// <summary>
/// Provides extension methods for <see cref="TimeSpan"/> objects.
/// </summary>
public static class TimeSpanExtensions
{
    /// <summary>
    /// Converts <see cref="TimeSpan"/> objects to a simple human-readable string.  Examples: 3.1 seconds, 2 minutes, 4.23 hours, etc.
    /// </summary>
    /// <param name="span">The timespan.</param>
    /// <param name="significantDigits">Significant digits to use for output.</param>
    /// <returns></returns>
    public static string ToHumanTimeString(this TimeSpan span, int significantDigits = 3)
    {
        if(span <= TimeSpan.Zero)
            return "0";
        
        var format = "G" + significantDigits;
        return span.TotalMilliseconds < 1000
            ? span.TotalMilliseconds.ToString(format) + " milliseconds"
            : span.TotalSeconds < 60
                ? span.TotalSeconds.ToString(format) + " seconds"
                : span.TotalMinutes < 60
                    ? span.TotalMinutes.ToString(format) + " minutes"
                    : span.TotalHours < 24
                        ? span.TotalHours.ToString(format) + " hours"
                        : span.TotalDays.ToString(format) + " days";
    }
}