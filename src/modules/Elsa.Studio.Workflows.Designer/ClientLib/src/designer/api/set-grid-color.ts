import {graphs} from "../internal/graphs";

export function setGridColor(graphId: string, color: string) {
    const graph = graphs[graphId];
    graph.grid.update({color: color});
}