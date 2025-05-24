import {Activity, ActivityStats} from "../models";

export const activityTagName = "elsa-activity-wrapper";

export function createActivityElement(activity: Activity, detached?: boolean, selectedPort?: string, stats?: ActivityStats): HTMLElement {
    const activityElement = document.createElement(activityTagName) as any;
    
    // Make sure we have a valid activity with an id
    if (!activity || !activity.id) {
        console.warn("Activity or activity.id is undefined or null");
        // Create a placeholder activity with a unique id to avoid reference tracking errors
        if (!activity) {
            activity = { id: `temp-${Date.now()}` } as Activity;
        } else if (!activity.id) {
            activity.id = `temp-${Date.now()}`;
        }
    }
    
    const activityId = activity.id;
    const elementId = `activity-${activityId}`;

    if(!detached) {
        activityElement.id = elementId;
        activityElement.setAttribute("element-id", elementId);
    }
    
    if(!!selectedPort) 
        activityElement.setAttribute("selected-port-name", selectedPort);
    
    activityElement.stats = stats;
    
    // Ensure activity is serialized safely
    try {
        activityElement.setAttribute("activity-json", JSON.stringify(activity));
    } catch (error) {
        console.warn("Error serializing activity:", error);
        // Provide a minimal safe version of the activity
        activityElement.setAttribute("activity-json", JSON.stringify({
            id: activityId,
            type: activity.type || "Unknown",
            version: activity.version || 1
        }));
    }
    
    activityElement.setAttribute("activity-id", activityId);
    
    return activityElement;
}