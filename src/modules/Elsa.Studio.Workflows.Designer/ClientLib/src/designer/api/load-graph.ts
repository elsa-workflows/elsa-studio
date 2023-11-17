import {Model} from '@antv/x6';
import {graphBindings} from "./graph-bindings";

export function loadGraph(graphId: string, data: string | Model.FromJSONData) {
    const {graph} = graphBindings[graphId];
    const model = typeof data === 'string' ? JSON.parse(data) : data;
    graph.fromJSON(model);
    graph.centerContent({padding: 20});
}