import {Graph, Shape} from '@antv/x6';
import {createActivityElement} from "./create-activity-element";
import {Activity} from "../models";

export function initialize() {
    const createPortInteractionAttrs = () => ({
        port: {
            magnet: true,
        },
        hitArea: {
            r: 12,
            fill: "transparent",
            stroke: "transparent",
            pointerEvents: "all",
            cursor: "crosshair",
        },
    });

    Shape.HTML.register({
        shape: "elsa-activity",
        effect: ["data", "activityStats"],
        html(cell) {
            const activity: Activity = cell.getData();
            const selectedPort = cell.prop('selected-port');
            const activityStats = cell.prop('activityStats');
            return createActivityElement(activity, false, selectedPort, activityStats);
        },
        portMarkup: [
            {
                tagName: "g",
                selector: "port",
                children: [
                    {
                        tagName: "circle",
                        selector: "hitArea",
                        className: "elsa-designer-port-hit-area",
                    },
                    {
                        tagName: "circle",
                        selector: "circle",
                        className: "elsa-designer-port-circle",
                    },
                ],
            },
        ],
        ports: {
            groups: {
                in: {
                    position: "left",
                    attrs: {
                        ...createPortInteractionAttrs(),
                        circle: {
                            r: 5,
                            stroke: "var(--elsa-designer-port-stroke)",
                            strokeWidth: 2,
                            fill: "var(--elsa-designer-port-surface)",
                            pointerEvents: "none",
                        },
                        text: {
                            fontSize: 12,
                            fill: "var(--elsa-designer-port-text)",
                        },
                    },
                    label: {
                        position: {
                            name: "outside",
                        },
                    },
                },
                out: {
                    position: "right",
                    attrs: {
                        ...createPortInteractionAttrs(),
                        circle: {
                            r: 5,
                            stroke: "var(--elsa-designer-port-surface)",
                            strokeWidth: 2,
                            fill: "var(--elsa-designer-port-stroke)",
                            pointerEvents: "none",
                        },
                        text: {
                            fontSize: 12,
                            fill: "var(--elsa-designer-port-text)",
                        },
                    },
                    label: {
                        position: {
                            name: "outside",
                        },
                    },
                },
            },
        }
    });

    Graph.registerEdge(
        'elsa-edge',
        {
            inherit: 'edge',
            attrs: {
                line: {
                    stroke: 'var(--elsa-designer-edge)',
                    strokeWidth: 1,
                    targetMarker: 'classic',
                    size: 6,
                },
            },
        },
        true,
    );

    Graph.registerEdge(
        'elsa-sequence-edge',
        {
            inherit: 'edge',
            attrs: {
                line: {
                    stroke: 'var(--elsa-designer-edge)',
                    strokeWidth: 2,
                    targetMarker: 'classic',
                    size: 6,
                },
            },
            connector: {
                name: 'rounded',
                args: {
                    radius: 8,
                },
            },
            router: {
                name: 'orth',
            },
        },
        true,
    );

}
