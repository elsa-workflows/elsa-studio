import {graphBindings} from "./graph-bindings";

export function disposeGraph(graphId: string) {
    const binding = graphBindings[graphId];
    if (!binding)
        return;

    binding.graph.dispose();
    delete graphBindings[graphId];
}
