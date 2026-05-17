namespace Elsa.Studio.Workflows.Designer;

/// <summary>
/// Contains shared constants used by the StateMachine designer.
/// </summary>
public static class StateMachineDesignerConstants
{
    /// <summary>
    /// Gets the marker property used for slot values that could not be parsed as JSON.
    /// </summary>
    public const string InvalidJsonSlotProperty = "$invalidJson";

    /// <summary>
    /// Gets the property that stores the original invalid JSON text.
    /// </summary>
    public const string InvalidJsonSlotSourceProperty = "source";
}
