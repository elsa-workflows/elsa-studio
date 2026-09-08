import { defineConfig } from 'vitest/config';

// There is no runtime test suite here (yet): the one thing this package needs verified is that
// src/bpmn/types.generated.ts actually describes the JSON Elsa's activity serializer produces for a
// real BpmnProcess.Process value. That is a compile-time question, so it is asked with a `.test-d.ts`
// file and Vitest's typecheck mode rather than a runtime assertion -- see
// src/bpmn/__tests__/process-payload.test-d.ts.
export default defineConfig({
  test: {
    include: [],
    typecheck: {
      enabled: true,
      include: ['src/**/*.test-d.ts'],
    },
  },
});
