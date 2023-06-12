import {Activity} from "../models";

export async function loadData() {

    const dummyActivity: Activity = {
        id: '1',
        type: 'WriteLine',
        version: 1,
        metadata: {
            displayName: 'Write line',
            description: 'Writes a line of text to the console',
            category: 'Control Flow',
            outcomes: ['Done']
        }
    };
    
    //const size = await calculateActivitySize(dummyActivity);
    const size = { width: 100, height: 100};

    return {
        nodes: [
            {
                id: 'node1', // String，可选，节点的唯一标识
                shape: 'elsa-activity',
                x: 40,       // Number，必选，节点位置的 x 值
                y: 40,       // Number，必选，节点位置的 y 值
                width: size.width,
                height: size.height,
                data: dummyActivity,
                ports: [{
                    id: "port1",
                    group: "in",
                }, {
                    id: "port2",
                    group: "out",
                }]
            },
            {
                id: 'node2', // String，节点的唯一标识
                shape: 'elsa-activity',
                x: 320,      // Number，必选，节点位置的 x 值
                y: 40,      // Number，必选，节点位置的 y 值
                width: size.width,
                height: size.height,
                data: dummyActivity,
                ports: [{
                    id: "port3",
                    group: "in",
                }, {
                    id: "port4",
                    group: "out",
                }]
            }
        ],
        edges: [
            {
                source: {
                    cell: 'node1',
                    port: 'port2',
                },
                target: {
                    cell: 'node2',
                    port: 'port3',
                },
                shape: 'elsa-edge'
            }
        ],
    };
}