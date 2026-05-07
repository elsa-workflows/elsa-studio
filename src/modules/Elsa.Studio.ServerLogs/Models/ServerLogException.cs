namespace Elsa.Studio.ServerLogs.Models;

/// <summary>
/// Exception details captured with a server log event.
/// </summary>
public class ServerLogException
{
    public string? Type { get; set; }
    public string? Message { get; set; }
    public string? StackTrace { get; set; }
}
