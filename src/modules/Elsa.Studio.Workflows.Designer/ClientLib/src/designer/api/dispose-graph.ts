import {graphBindings} from "./graph-bindings";

export function disposeGraph(graphId) {
    const binding = graphBindings[graphId];
    binding.graph.dispose();
    delete graphBindings[graphId];
}