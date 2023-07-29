import {WorkflowDefinitionEditor} from "../components/WorkflowDefinitionEditor/WorkflowDefinitionEditor.jsx";

export default {
    title: "Workflows/WorkflowDefinitionEditor",
    component: WorkflowDefinitionEditor,
    parameters: {
        layout: "centered"
    },
    argTypes: {
        definitionId: { control: "text" }
    }
};

export const Default = {
    args: {
        definitionId: "98e9523c2c8642fea4c652018e684b0d"
    }
}