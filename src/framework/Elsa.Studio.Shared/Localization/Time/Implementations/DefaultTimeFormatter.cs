using System.Globalization;

namespace Elsa.Studio.Localization.Time;

/// <summary>
/// A default implementation that leverages the currently installed <see cref="ITimeZoneProvider"/>.
/// </summary>
public class DefaultTimeFormatter(ITimeZoneProvider timeZoneProvider) : ITimeFormatter
{
    /// <inheritdoc />
    public string Format(DateTimeOffset value, string format)
    {
        var timeZone = timeZoneProvider.GetTimeZone();
        var localTime = TimeZoneInfo.ConvertTime(value, timeZone);
        return localTime.ToString(format, CultureInfo.CurrentCulture);
    }
}