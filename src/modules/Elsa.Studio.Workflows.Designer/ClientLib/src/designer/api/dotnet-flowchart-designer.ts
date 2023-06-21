import {DotNetComponentRef} from "./graph-bindings";
import {Activity} from "../models";

export class DotNetFlowchartDesigner {
    constructor(private componentRef: DotNetComponentRef) {
    }

    /// <summary>
    /// Raises the <see cref="ActivitySelected"/> event.
    /// </summary>
    async raiseActivitySelected(activity: Activity) : Promise<void> {
        await this.componentRef.invokeMethodAsync('HandleActivitySelected', activity);
    }
    
    /// <summary>
    /// Raises the <see cref="CanvasSelected"/> event.
    /// </summary>
    async raiseCanvasSelected() : Promise<void> {
        await this.componentRef.invokeMethodAsync('HandleCanvasSelected');
    }
}