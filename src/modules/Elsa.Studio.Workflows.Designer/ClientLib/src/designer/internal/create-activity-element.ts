import {Activity} from "../models";

export const activityTagName = "elsa-activity-wrapper";

export function createActivityElement(activity: Activity, detached?: boolean, selectedPort?: string): HTMLElement {
    const activityElement = document.createElement(activityTagName);
    const activityId = activity.id;
    const elementId = `activity-${activityId}`;

    if(!detached) {
        activityElement.id = elementId;
        activityElement.setAttribute("element-id", elementId);
    }
    
    if(!!selectedPort) {
        activityElement.setAttribute("selected-port-name", selectedPort);
    }

    (activityElement as any).activity = activity;
    activityElement.setAttribute("activity-id", activityId);
    
    return activityElement;
}