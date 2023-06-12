import {activityTagName, createActivityElement} from "../internal/create-activity-element";
import {Activity, Size} from "../models";

export function calculateActivitySize(activity: Activity): Promise<Size> {
    debugger;
    const activityId = activity.id;
    const wrapper = document.createElement('div');
    const dummyActivityElement = createActivityElement(activity, true);
    wrapper.style.position = 'absolute';
    wrapper.appendChild(dummyActivityElement);

    // Append the temporary element to the DOM.
    const bodyElement = document.getElementsByTagName('body')[0];
    bodyElement.append(wrapper);

    // Wait for activity element to be completely rendered.
    // When using custom elements, they are rendered after they are mounted. Before then, they have a 0 width and height.
    return new Promise((resolve, reject) => {
        const checkSize = () => {
            const activityElement: Element = wrapper.getElementsByTagName(activityTagName)[0];
            const activityElementRect = activityElement.getBoundingClientRect();

            // If the custom element has no width or height yet, it means it has not yet rendered.
            if (activityElementRect.width == 0 || activityElementRect.height == 0) {
                // Request an animation frame and call ourselves back immediately after.
                window.requestAnimationFrame(checkSize);
            } else {
                const rect = wrapper.firstElementChild.getBoundingClientRect();
                const width = rect.width;
                const height = rect.height;

                // Remove the temporary element (used only to calculate its size).
                wrapper.remove();

                // Update size of the activity node and resolve the promise.
                resolve({width, height});
            }
        };

        // Begin try to get our element size.
        checkSize();
    });
}