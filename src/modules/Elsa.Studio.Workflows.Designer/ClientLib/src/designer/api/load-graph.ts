import {Model, Graph} from '@antv/x6';
import {graphBindings} from "./graph-bindings";

export function loadGraph(graphId: string, model: Model.FromJSONData) {
    const {graph} = graphBindings[graphId];
    graph.fromJSON(model);
    graph.centerContent({padding: 20});
}