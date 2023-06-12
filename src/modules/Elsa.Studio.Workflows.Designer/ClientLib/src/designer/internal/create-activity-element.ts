export const activityTagName = "elsa-activity-wrapper";

export function createActivityElement(activityId: string, detached: boolean): HTMLElement {
    const activityElement = document.createElement(activityTagName);
    const elementId = `activity-${activityId}`;
    
    if(!detached) {
        activityElement.id = elementId;
        activityElement.setAttribute("element-id", elementId);
    }
    
    activityElement.setAttribute("activity-id", activityId);
    return activityElement;
}