import {graphBindings} from "./graph-bindings";

export function setGridColor(graphId: string, color: string) {
    const {graph} = graphBindings[graphId];
    graph.grid.update({color: color});
}