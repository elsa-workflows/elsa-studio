import {DotNetComponentRef} from "./graph-bindings";
import {Activity} from "../models";

export class DotNetFlowchartDesigner {
    constructor(private componentRef: DotNetComponentRef) {
    }

    /// <summary>
    /// Raises the <see cref="ActivitySelected"/> event.
    /// </summary>
    async raiseActivitySelected(activity: Activity): Promise<void> {
        console.debug('ActivitySelected');
        await this.componentRef.invokeMethodAsync('HandleActivitySelected', activity);
    }

    /// <summary>
    /// Raises the <see cref="ActivitySelected"/> event.
    /// </summary>
    async raiseActivityEmbeddedPortSelected(activity: Activity, portName: string): Promise<void> {
        console.debug('ActivityEmbeddedPortSelected');
        await this.componentRef.invokeMethodAsync('HandleActivityEmbeddedPortSelected', activity, portName);
    }

    /// <summary>
    /// Raises the <see cref="ActivityDoubleClick"/> event.
    /// </summary>
    async raiseActivityDoubleClick(activity: Activity): Promise<void> {
        console.debug('ActivityDoubleClick');
        await this.componentRef.invokeMethodAsync('HandleActivityDoubleClick', activity);
    }

    /// <summary>
    /// Raises the <see cref="CanvasSelected"/> event.
    /// </summary>
    async raiseCanvasSelected(): Promise<void> {
        console.debug('CanvasSelected');
        await this.componentRef.invokeMethodAsync('HandleCanvasSelected');
    }

    /// <summary>
    /// Raises the <see cref="GraphUpdated"/> event.
    /// </summary>
    async raiseGraphUpdated(): Promise<void> {
        console.debug('GraphUpdated');
        await this.componentRef.invokeMethodAsync('HandleGraphUpdated');
    }

    /// <summary>
    /// Raises the <see cref="PasteCellsRequested"/> event.
    /// </summary>
    async raisePasteCellsRequested(activityCells: any[], edgeCells: any[]): Promise<void> {
        console.debug('PasteCellsRequested');
        await this.componentRef.invokeMethodAsync('HandlePasteCellsRequested', activityCells, edgeCells);
    }
}