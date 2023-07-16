import {Node} from "@antv/x6";
import {Activity} from "../models";
import {graphBindings} from "./graph-bindings";
import {Ports} from "../models/x6";

export async function updateActivity(graphId: string, activityId: string, activity: Activity, ports?: Ports) {
    debugger;
    // Get graph reference.
    const {graph, interop} = graphBindings[graphId];

    // Get the node from the graph.
    let node = graph.getNodes().find(x => x.id == activityId);

    // Update the node data.
    if (!!node) {

        // Update the node's data with the activity.
        node.setData(activity, {overwrite: true});

        // If the ID of the activity changed, we need to update the graph model.
        if (activityId !== activity.id) {
            graph.model.updateCellId(node, activity.id);
        }
        
        // Update ports.
        if (!!ports) {
            updatePorts(node, ports);
        }
        
        // Publish changed event.
        await interop.raiseGraphUpdated();
    }
}

const updatePorts = (node: Node<Node.Properties>, ports: Ports) => {
    const desiredPorts = ports.items;
    const actualPorts = node.ports.items;
    const addedPorts = desiredPorts.filter(x => !actualPorts.some(y => y.id == x.id));
    const removedPorts = actualPorts.filter(x => !desiredPorts.some(y => y.id == x.id));

    if (addedPorts.length > 0)
        node.addPorts(addedPorts);

    if (removedPorts.length > 0)
        node.removePorts(removedPorts);
};