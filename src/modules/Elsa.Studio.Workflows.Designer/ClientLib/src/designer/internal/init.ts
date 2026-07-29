import {Graph, Shape} from '@antv/x6';
import {createActivityElement} from "./create-activity-element";
import {Activity} from "../models";

export function initialize() {

    Shape.HTML.register({
        shape: "elsa-activity",
        effect: ["data", "activityStats"],
        html(cell) {
            const activity: Activity = cell.getData();
            const selectedPort = cell.prop('selected-port');
            const activityStats = cell.prop('activityStats');
            return createActivityElement(activity, false, selectedPort, activityStats);
        },
        ports: {
            groups: {
                in: {
                    position: "left",
                    attrs: {
                        circle: {
                            r: 5,
                            magnet: true,
                            stroke: "var(--elsa-designer-port-stroke)",
                            strokeWidth: 2,
                            fill: "var(--elsa-designer-port-surface)",
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
                        circle: {
                            r: 5,
                            magnet: true,
                            stroke: "var(--elsa-designer-port-surface)",
                            strokeWidth: 2,
                            fill: "var(--elsa-designer-port-stroke)",
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
