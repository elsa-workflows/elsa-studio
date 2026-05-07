namespace Elsa.Studio.ServerLogs.Models;

/// <summary>
/// Log levels emitted by the backend server log stream.
/// </summary>
public enum ServerLogLevel
{
    Trace = 0,
    Debug = 1,
    Information = 2,
    Warning = 3,
    Error = 4,
    Critical = 5,
    None = 6
}
