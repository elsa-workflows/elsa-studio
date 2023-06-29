import {calculateActivitySize} from "./calculate-activity-size";
import {Activity} from "../models";
import {graphBindings} from "./graph-bindings";

export async function updateActivitySize(elementId: string, activity: Activity) {
    // Get wrapper element.
    const wrapper = document.getElementById(elementId);

    // Get container element.
    const container = wrapper.closest('.graph-container');

    // Get graph ID.
    const graphId = container.id;

    // Get graph reference.
    const {graph} = graphBindings[graphId];

    // Calculate the size of the activity.
    const rect = await calculateActivitySize(activity);
    const width = rect.width;
    const height = rect.height;

    // Get the node from the graph and update its size.
    const activityId = activity.id;
    const node = graph.getNodes().find(x => x.id == activityId);
    node.size(width, height);
}