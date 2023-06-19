import {DotNetComponentRef} from "./graph-bindings";

export class DotNetFlowchartDesigner {
    constructor(private componentRef: DotNetComponentRef) {
    }

    /// <summary>
    /// Raises the <see cref="ActivitySelected"/> event.
    /// </summary>
    async raiseActivitySelected(activity: any) : Promise<void> {
        debugger;
        await this.componentRef.invokeMethodAsync('OnActivitySelected', activity);
    }
}