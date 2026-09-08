/**
 * Type-checks a real serialized `BpmnProcess.Process` payload against the generated types, with no
 * cast anywhere in the assignment below. If `BpmnProcessDefinition` in ../types.generated stops
 * describing the JSON Elsa's activity serializer actually produces, `npm test` fails to compile this
 * file -- that is the whole test.
 *
 * ../__fixtures__/camunda-order-process.process.json is not a hand-written example: it is the exact
 * value of the `"process"` property from the `Elsa.BpmnProcess` activity that
 * `Bpmn.Interchange.BpmnInterchangeDocumentService.ImportAsync` produces for
 * `test/integration/Elsa.Bpmn.Interchange.IntegrationTests/Assets/camunda-order-process.bpmn` in
 * elsa-core, captured by asking `Elsa.Workflows.Serialization.Serializers.JsonActivitySerializer` --
 * the same serializer a GET of a workflow definition goes through -- to serialize the resulting
 * workflow's root activity, then lifting out the `"process"` property. It was NOT captured through
 * the HTTP import endpoint mentioned in studio issue #997, because doing that needs a running
 * elsa-core host, which is outside this repository's test suite; this is the same wire format the
 * endpoint produces, reached through the library code the endpoint itself calls.
 *
 * To reproduce: with elsa-core checked out alongside this repo, run a small program that
 *   1. builds an `IServiceCollection` with `.AddElsa(elsa => elsa.UseWorkflowManagement().UseBpmnInterchange())`,
 *   2. calls `provider.PopulateRegistriesAsync()` (Elsa.Testing.Shared.Integration),
 *   3. resolves `BpmnInterchangeDocumentService` and calls
 *      `ImportAsync(xml, definitionId: null, name: null, processId: null, cancellationToken)` with the
 *      asset's XML,
 *   4. parses `result.ImportResult.WorkflowDefinition.StringData` as JSON and writes out its
 *      `"process"` property.
 */
import type { BpmnProcessDefinition } from '../types.generated';
import processFixture from '../__fixtures__/camunda-order-process.process.json';

const typedProcess: BpmnProcessDefinition = processFixture;
void typedProcess;
