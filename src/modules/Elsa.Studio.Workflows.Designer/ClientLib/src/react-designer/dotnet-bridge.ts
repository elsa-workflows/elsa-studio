import type { DotNetComponentRef, ElsaActivity } from './types';

// Mirrors DotNetFlowchartDesigner so the C# side keeps the same JSInvokable
// surface (HandleActivitySelected, HandleActivityDoubleClick, ...).
export class DotNetReactDesigner {
    constructor(private readonly componentRef: DotNetComponentRef) {}

    raiseActivitySelected(activity: ElsaActivity): Promise<void> {
        return this.componentRef.invokeMethodAsync('HandleActivitySelected', activity);
    }

    raiseActivityDoubleClick(activity: ElsaActivity): Promise<void> {
        return this.componentRef.invokeMethodAsync('HandleActivityDoubleClick', activity);
    }

    raiseActivityEmbeddedPortSelected(activity: ElsaActivity, portName: string): Promise<void> {
        return this.componentRef.invokeMethodAsync('HandleActivityEmbeddedPortSelected', activity, portName);
    }

    raiseCanvasSelected(): Promise<void> {
        return this.componentRef.invokeMethodAsync('HandleCanvasSelected');
    }

    raiseGraphUpdated(): Promise<void> {
        return this.componentRef.invokeMethodAsync('HandleGraphUpdated');
    }

    raisePasteCellsRequested(activityCells: any[], edgeCells: any[]): Promise<void> {
        return this.componentRef.invokeMethodAsync('HandlePasteCellsRequested', activityCells, edgeCells);
    }
}
