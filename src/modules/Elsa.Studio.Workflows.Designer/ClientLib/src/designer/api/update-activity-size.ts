import {calculateActivitySize} from "./calculate-activity-size";
import {Activity, Size} from "../models";
import {graphBindings} from "./graph-bindings";
import {Node} from "@antv/x6";

export async function updateActivitySize(elementId: string, activityModel: Activity | string, size?: Size, portCount?: number) {
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
    const rect = await calculateActivitySize(activity, portCount);
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

/**
 * Enforces the minimum size on a node based on its activity content.
 * If the current size is smaller than the minimum, the node will snap back to the minimum size.
 * @param node The X6 node to enforce minimum size on
 * @returns true if the size was adjusted, false otherwise
 */
export async function enforceMinimumNodeSize(node: Node): Promise<boolean> {
    const activity: Activity = node.data;
    
    if (!activity) {
        return false;
    }

    // Calculate the minimum size based on content
    const minSize = await calculateActivitySize(activity);
    const currentSize = node.size();
    
    let needsResize = false;
    let newWidth = currentSize.width;
    let newHeight = currentSize.height;
    
    if (currentSize.width < minSize.width) {
        newWidth = minSize.width;
        needsResize = true;
    }
    
    if (currentSize.height < minSize.height) {
        newHeight = minSize.height;
        needsResize = true;
    }
    
    if (needsResize) {
        node.size(newWidth, newHeight);
    }
    
    return needsResize;
}
