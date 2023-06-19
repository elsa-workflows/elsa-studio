import {graphBindings} from "./graph-bindings";

export function centerContent(graphId: string) {
    const {graph} = graphBindings[graphId];
    
    graph.centerContent({
        padding: 20,
        useCellGeometry: true
    });
}