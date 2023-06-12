import {Model, Graph} from '@antv/x6';
import {graphs} from "../internal/graphs";

export function loadGraph(graphId: string, json: Model.FromJSONData) {
    const graph : Graph = graphs[graphId];
    graph.fromJSON(json);
    graph.centerContent({padding: 20});
}