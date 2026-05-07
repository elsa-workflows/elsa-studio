import { reactBindings } from '../bindings';

export function selectReactActivity(graphId: string, activityId: string): void {
    reactBindings[graphId]?.selectNode(activityId);
}
