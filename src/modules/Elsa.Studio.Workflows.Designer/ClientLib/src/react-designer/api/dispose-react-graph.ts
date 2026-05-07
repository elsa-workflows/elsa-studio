import { reactBindings } from '../bindings';

export function disposeReactGraph(graphId: string): void {
    const binding = reactBindings[graphId];
    if (!binding) return;
    try {
        binding.root.unmount();
    } catch {
        // Ignore unmount errors during page tear-down.
    }
    delete reactBindings[graphId];
}
