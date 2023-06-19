import {ActivityDisplayTextMetadata} from "./metadata";

export interface Activity {
    id: string;
    type: string;
    version: number;
    metadata: any | ActivityDisplayTextMetadata;
    canStartWorkflow?: boolean;
    runAsynchronously?: boolean;
    customProperties?: any;

    [name: string]: any;
}

