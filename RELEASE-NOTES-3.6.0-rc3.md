# Release Notes: Elsa Studio 3.6.0-rc3

**Compare**: `3.6.0-rc2...3.6.0-rc3`

## 🐛 Fixes

### Workflow List Pagination

- **Workflow Definitions list**: Fixed pagination reset to first page after selecting a workflow definition from subsequent pages. Preserved current page state during re-renders in `WorkflowDefinitionList`. (11d628f23a) (#735)
  
- **Query parameter handling**: Refactored query parameter parsing logic in `QueryTableComponentBase` for better maintainability. Uses properties instead of fields for initial page and page size, improving state management across workflow components. (af8442871c) (#736)

### Workflow Designer

- **Node sizing**: Added minimum node size enforcement to ensure workflow designer nodes meet minimum size requirements based on activity content. Enhanced activity size caching by incorporating display text and description lengths for improved accuracy. (0b02c672571) (#733)

### Workflow Instance Viewer

- **Workflow-as-activity instances**: Fixed two navigation issues in workflow instance viewer:
  - Double-clicking workflow-as-activity instances now correctly navigates to sub-workflow instances by restoring `IsReadOnly` parameter initialization.
  - Journal item navigation outside current container/flowchart now properly loads activities by implementing fallback node loading when activity is not immediately found. (2fd57289f4) (#727)

- **Auto layout**: Made "Auto layout" toolbox item consistently available in `FlowchartDiagramDesigner` regardless of read-only state.

## 📦 Dependencies

- **Elsa.Api.Client**: Updated to version 3.6.0-rc3. (e36fbdc692)

## 📦 Full changelog

All changes in this release have been documented above.
