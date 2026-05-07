import { reactBindings } from '../bindings';

export function autoLayoutReactGraph(graphId: string): void {
    reactBindings[graphId]?.autoLayout();
}
