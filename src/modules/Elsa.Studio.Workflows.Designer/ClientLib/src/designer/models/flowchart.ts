import {Activity} from "./activity";
import {Connection} from "./connection";

export interface Flowchart extends Activity {
    activities: Array<Activity>;
    connections: Array<Connection>;
    start: string;
}