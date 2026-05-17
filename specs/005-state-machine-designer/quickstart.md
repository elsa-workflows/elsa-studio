# Quickstart: State Machine Designer

## Prerequisites

- Use the Studio worktree branch `006-state-machine-designer`.
- Use a backend build that includes elsa-core issue #5085 / PR #7457 StateMachine activity support.
- Confirm Studio's activity registry receives a descriptor for `Elsa.StateMachine`.

## Scenario 1: Inspect an Existing StateMachine

1. Start the Studio sample host.
2. Connect it to a backend that advertises `Elsa.StateMachine`.
3. Open a workflow definition containing a StateMachine with at least three states and two transitions.
4. Select or drill into the StateMachine activity from the workflow editor.
5. Verify a dedicated State Machine designer opens.
6. Verify each state appears once and each transition is drawn from source to target.
7. Verify terminal states are visually identifiable.
8. Use zoom-to-fit and center controls.
9. Return to the root Flowchart and verify normal Flowchart selection, drag/drop, and zoom behavior still works.

## Scenario 2: Edit Graph Shape

1. Open a workflow with an empty or small StateMachine.
2. Add two states named `NewOrder` and `Paid`.
3. Set `NewOrder` as the initial state.
4. Add a transition from `NewOrder` to `Paid`.
5. Save and reload the workflow definition.
6. Verify `states`, `transitions`, `initialState`, and nested slot payloads remain intact.

## Scenario 3: Edit Transition Slots

1. Select a transition edge.
2. Set the transition display name.
3. Drag an activity from the existing activity picker onto the trigger slot, or paste a valid trigger activity JSON payload.
4. Edit the condition JSON payload.
5. Drag an activity from the existing activity picker onto the action slot, or paste a valid action activity JSON payload.
6. Save and reload the workflow definition.
7. Verify the trigger, condition, and action remain attached to the same transition.

## Scenario 4: Validate Broken References

1. Load a StateMachine with a transition pointing to a missing state.
2. Verify the graph still renders partial content.
3. Verify Studio shows a blocking validation issue for the broken reference.
4. Fix the target state or remove the transition.
5. Verify the validation issue clears before save.

## Scenario 5: Edit State Slots

1. Select a state node.
2. Drag an activity from the existing activity picker onto the entry slot.
3. Drag an activity from the existing activity picker onto the exit slot.
4. Save and reload the workflow definition.
5. Verify the entry and exit payloads remain attached to the same state.

## Lightweight Validation Commands

```bash
dotnet build src/modules/Elsa.Studio.Workflows.Designer/Elsa.Studio.Workflows.Designer.csproj
dotnet build src/modules/Elsa.Studio.Workflows/Elsa.Studio.Workflows.csproj
dotnet test src/modules/Elsa.Studio.Workflows.Designer.Tests/Elsa.Studio.Workflows.Designer.Tests.csproj
dotnet test src/modules/Elsa.Studio.Workflows.Tests/Elsa.Studio.Workflows.Tests.csproj
```

The test command applies after the mapper test project exists.
