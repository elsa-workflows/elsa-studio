import {Activity} from "../models";
import {graphBindings} from "./graph-bindings";

export async function raiseActivityEmbeddedPortSelected(elementId: string, activity: Activity, portName: string) {
    // Get wrapper element.
    const wrapper = document.getElementById(elementId);

    // Get container element.
    const container = wrapper.closest('.graph-container');

    // Get graph ID.
    const graphId = container.id;

    // Get graph reference.
    const {interop} = graphBindings[graphId];
    
    await interop.raiseActivityEmbeddedPortSelected(activity, portName);
}