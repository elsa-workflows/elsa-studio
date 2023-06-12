import {graphs} from "../internal/graphs";
import {calculateActivitySize} from "./calculate-activity-size";

export async function updateActivityNode(elementId: string, activityId: string) {
    // Get wrapper element.
    const wrapper = document.getElementById(elementId);

    // Get container element.
    const container = wrapper.closest('.graph-container');

    // Get graph ID.
    const graphId = container.id;

    // Get graph reference.
    const graph = graphs[graphId];
    
    // Calculate the size of the activity.
    const rect = await calculateActivitySize(activityId);
    const width = rect.width;
    const height = rect.height;

    // Get the node from the graph and update its size.
    const node = graph.getNodes().find(x => x.id == activityId);
    node.size(width, height);
}