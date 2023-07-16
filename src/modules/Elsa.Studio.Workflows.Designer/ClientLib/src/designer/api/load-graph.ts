import {Model, Graph} from '@antv/x6';
import {graphBindings} from "./graph-bindings";

export function loadGraph(graphId: string, json: string) {
    const {graph} = graphBindings[graphId];
    const model = JSON.parse(json) as Model.FromJSONData;
    graph.fromJSON(model);
    graph.centerContent({padding: 20});
}