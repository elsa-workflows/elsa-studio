import {graphBindings} from "./graph-bindings";

export function zoomToFit(graphId: string) {
    const {graph} = graphBindings[graphId];
    graph.zoomToFit({
        padding: 20,
        minScale: 0.5,
        maxScale: 3
    });
}