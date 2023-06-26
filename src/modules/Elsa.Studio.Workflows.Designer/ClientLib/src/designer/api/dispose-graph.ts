import {graphBindings} from "./graph-bindings";

export function disposeGraph(graphId) {
    delete graphBindings[graphId];
}