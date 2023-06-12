import {graphs} from "../internal/graphs";

export function disposeGraph(graphId) {
    const graph = graphs[graphId];
    graph.dispose();
    delete graphs[graphId];
}