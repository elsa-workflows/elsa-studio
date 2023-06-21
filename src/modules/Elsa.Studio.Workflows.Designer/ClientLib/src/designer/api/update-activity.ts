import {Activity} from "../models";
import {graphBindings} from "./graph-bindings";

export async function updateActivity(graphId: string, activity: Activity) {
    // Get graph reference.
    const {graph} = graphBindings[graphId];

    // Get the node from the graph.
    const activityId = activity.id;
    const node = graph.getNodes().find(x => x.id == activityId);

    // Update the node data.
    if (!!node) {

        // Update the node's data with the activity.
        node.setData(activity, {overwrite: true});
    }
}