import { WorkflowInstanceViewer} from "../components/WorkflowInstanceViewer/WorkflowInstanceViewer.jsx";
import {RemoteArgs} from "../remote-args.js";

export default {
    title: "Workflows/WorkflowInstanceViewer",
    component: WorkflowInstanceViewer,
    parameters: {
        layout: "centered"
    },
    argTypes: {
        definitionId: { control: "text" }
    }
};

export const Default = {
    args: {
        instanceId: "a015a655bdd4ec0a",
        remoteEndpoint: RemoteArgs.remoteEndpoint,
        apiKey: RemoteArgs.apiKey
    }
}