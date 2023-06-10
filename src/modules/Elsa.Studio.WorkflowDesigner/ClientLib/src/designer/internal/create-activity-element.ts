export const activityTagName = "elsa-activity-wrapper";

export function createActivityElement(activity): HTMLElement {
    console.debug("custom-html");
    const activityElement = document.createElement(activityTagName);
    activityElement.setAttribute("label", activity.label);
    return activityElement;
}
