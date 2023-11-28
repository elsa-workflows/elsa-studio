import {calculateActivitySize} from "./calculate-activity-size";
import {Activity, Size} from "../models";
import {graphBindings} from "./graph-bindings";

export async function updateActivitySize(elementId: string, activityModel: Activity | string, size?: Size) {
    // Get wrapper element.
    const wrapper = document.getElementById(elementId);

    if (wrapper == null) {
        console.warn(`Could not find wrapper element with ID ${elementId}`);
        return;
    }

    // Get container element.
    const container = wrapper.closest('.graph-container');

    // Get graph ID.
    const graphId = container.id;

    // Get graph reference.
    const {graph} = graphBindings[graphId];
    
    // Parse activity model.
    const activity = typeof activityModel === 'string' ? JSON.parse(activityModel) : activityModel;

    // Calculate the size of the activity.
    const rect = await calculateActivitySize(activity);
    let width = rect.width;
    let height = rect.height;

    // Get the node from the graph and update its size.
    const activityId = activity.id;
    const node = graph.getNodes().find(x => x.id == activityId);

    if (node == null) {
        console.warn(`Could not find node with ID ${activityId} in graph ${graphId}`);
        return;
    }

    if (!!size) {
        if (size.width > width)
            width = size.width;

        if (size.height > height)
            height = size.height;
    }
    
    node.size(width, height);
}