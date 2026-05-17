namespace Elsa.Studio.Workflows.Designer;

/// <summary>
/// Contains shared constants used by the StateMachine designer.
/// </summary>
public static class StateMachineDesignerConstants
{
    /// <summary>
    /// Gets the validation code used when the StateMachine states value is not a JSON array.
    /// </summary>
    public const string InvalidStateCollectionCode = "InvalidStateCollection";

    /// <summary>
    /// Gets the validation code used when the StateMachine transitions value is not a JSON array.
    /// </summary>
    public const string InvalidTransitionCollectionCode = "InvalidTransitionCollection";

    /// <summary>
    /// Gets the validation code used when a StateMachine state item is not a JSON object.
    /// </summary>
    public const string InvalidStateItemCode = "InvalidStateItem";

    /// <summary>
    /// Gets the validation code used when a StateMachine transition item is not a JSON object.
    /// </summary>
    public const string InvalidTransitionItemCode = "InvalidTransitionItem";

    /// <summary>
    /// Gets the marker property used for slot values that could not be parsed as JSON.
    /// </summary>
    public const string InvalidJsonSlotProperty = "$invalidJson";

    /// <summary>
    /// Gets the property that stores the original invalid JSON text.
    /// </summary>
    public const string InvalidJsonSlotSourceProperty = "source";
}
