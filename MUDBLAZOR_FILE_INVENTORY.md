# MudBlazor Usage File Inventory

> **Quick Reference**: Comprehensive list of all files using MudBlazor in Elsa Studio  
> **Purpose**: Enable quick navigation and assignment of migration tasks  
> **Version**: 1.0  
> **Date**: 2026-01-06

---

## Summary Statistics

- **Total Files**: 143 files with MudBlazor usage
- **_Imports.razor**: 14 files
- **C# Files**: 49 files
- **Razor Components**: 80+ files
- **HTML/CSHTML**: 6 host files
- **Component Instances**: 759 total

---

## 1. Package References

### Directory.Packages.props
```
/home/runner/work/elsa-studio/elsa-studio/Directory.Packages.props
```
- MudBlazor: 8.15.0
- CodeBeam.MudBlazor.Extensions: 8.3.0
- MudBlazor.Translations: 2.7.0

### Project Files

#### Core Framework
```
src/framework/Elsa.Studio.Core/Elsa.Studio.Core.csproj
```
- Direct reference to MudBlazor package
- Direct reference to CodeBeam.MudBlazor.Extensions package

#### Localization Module
```
src/modules/Elsa.Studio.Localization/Elsa.Studio.Localization.csproj
```
- Direct reference to MudBlazor.Translations package

---

## 2. Import Statements (_Imports.razor)

### Framework Level (2 files)

#### Shared Framework
```
src/framework/Elsa.Studio.Shared/_Imports.razor
```
**Imports**: `@using MudBlazor`

**Dependent Components**:
- Layouts/MainLayout.razor
- Layouts/BasicLayout.razor
- Components/CodeEditorDialog.razor
- Components/ExpressionEditor.razor
- Components/ExpressionInput.razor
- Components/DataPanel.razor
- Components/ContentVisualizer.razor
- Components/MarkdownEditor.razor
- Components/NavMenuItem.razor
- Components/FieldExtension.razor
- Components/ClientPackageVersion.razor
- Components/AppBar/*.razor
- Components/Unauthorized.razor

#### Shell Framework
```
src/framework/Elsa.Studio.Shell/_Imports.razor
```
**Imports**: `@using MudBlazor`

**Dependent Components**:
- App.razor

### Module Level (9 files)

#### Login Module
```
src/modules/Elsa.Studio.Login/_Imports.razor
```
**Imports**: `@using MudBlazor`

**Components**: 1 file
- Pages/Login/Login.razor

#### Dashboard Module
```
src/modules/Elsa.Studio.Dashboard/_Imports.razor
```
**Imports**: `@using MudBlazor`

**Components**: Dashboard pages

#### Security Module
```
src/modules/Elsa.Studio.Security/_Imports.razor
```
**Imports**: `@using MudBlazor`

**Components**: Security management pages

#### Counter Module
```
src/modules/Elsa.Studio.Counter/_Imports.razor
```
**Imports**: `@using MudBlazor`

**Components**: Counter demo page

#### Localization Module
```
src/modules/Elsa.Studio.Localization/_Imports.razor
```
**Imports**: `@using MudBlazor`

**Components**: Localization management pages

#### Workflows Module
```
src/modules/Elsa.Studio.Workflows/_Imports.razor
```
**Imports**: `@using MudBlazor`

**Components**: 40+ files (largest module)
- WorkflowDefinitionList.razor
- WorkflowInstanceList.razor
- CreateWorkflowDialog.razor
- CloneWorkflowDialog.razor
- WorkflowEditor.razor
- All property tabs and panels

#### Workflows Designer Module
```
src/modules/Elsa.Studio.Workflows.Designer/_Imports.razor
```
**Imports**: `@using MudBlazor`

**Components**: Designer-specific components
- FlowchartDesigner.razor
- ActivityWrapper.razor (V1 and V2)

#### UIHints Module
```
src/modules/Elsa.Studio.UIHints/_Imports.razor
```
**Imports**: `@using MudBlazor` AND `@using MudExtensions`

**Components**: 20+ input components
- Cases.razor
- Code.razor
- Dictionary.razor
- Dropdown.razor (MudExtensions)
- OutcomePicker.razor (MudExtensions)
- VariablePicker.razor (MudExtensions)
- InputPicker.razor (MudExtensions)
- OutputPicker.razor (MudExtensions)
- TypePicker.razor (MudExtensions)
- DateTimePicker.razor
- MultiText.razor
- HttpStatusCodes.razor
- DynamicOutcomes.razor
- SingleLine.razor
- CheckList.razor
- etc.

#### Environments Module
```
src/modules/Elsa.Studio.Environments/_Imports.razor
```
**Note**: Does NOT import MudBlazor in _Imports.razor

**Direct Usage**: EnvironmentPicker.razor has `@using MudBlazor` inline

### Host Level (1 file)

#### Custom Elements Host
```
src/hosts/Elsa.Studio.Host.CustomElements/_Imports.razor
```
**Imports**: `@using MudBlazor`

### Samples (1 file)

#### BlazorApp1 Sample
```
samples/BlazorApp1/Components/_Imports.razor
```
**Imports**: `@using MudBlazor`

---

## 3. CSS and JavaScript References

### Blazor WebAssembly Host
```
src/hosts/Elsa.Studio.Host.Wasm/wwwroot/index.html
```
**Line 13**: `<link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />`  
**Line 14**: `<link href="_content/CodeBeam.MudBlazor.Extensions/MudExtensions.min.css" rel="stylesheet" />`  
**Line 34**: `<script src="_content/MudBlazor/MudBlazor.min.js"></script>`  
**Line 35**: `<script src="_content/CodeBeam.MudBlazor.Extensions/MudExtensions.min.js"></script>`  

**Loading Splash** (Line 20-23):
```html
<div class="loading-splash mud-container mud-container-maxwidth-false">
    <h5 class="mud-typography mud-typography-h5 mud-primary-text my-6">Loading...</h5>
</div>
```

### Blazor Server Host
```
src/hosts/Elsa.Studio.Host.Server/Pages/_Host.cshtml
```
**CSS**: Lines with MudBlazor.min.css and MudExtensions.min.css  
**JS**: Lines with MudBlazor.min.js and MudExtensions.min.js  
**Loading Splash**: MudBlazor CSS classes

### Hosted WebAssembly Host
```
src/hosts/Elsa.Studio.Host.HostedWasm/Pages/_Host.cshtml
```
**CSS**: Lines with MudBlazor.min.css and MudExtensions.min.css  
**JS**: Lines with MudBlazor.min.js and MudExtensions.min.js  
**Loading Splash**: MudBlazor CSS classes

### Custom Elements Host
```
src/hosts/Elsa.Studio.Host.CustomElements/wwwroot/index.html
```
**CSS**: Lines with MudBlazor.min.css and MudExtensions.min.css  
**JS**: Lines with MudBlazor.min.js and MudExtensions.min.js  
**Loading Splash**: MudBlazor CSS classes

### BlazorApp1 Sample
```
samples/BlazorApp1/Components/App.razor
```
**Line 16**: `<link href="./_content/MudBlazor/MudBlazor.min.css" rel="stylesheet"/>`  
**Line 16**: `<link href="./_content/CodeBeam.MudBlazor.Extensions/MudExtensions.min.css" rel="stylesheet"/>`  
**Line 28**: `<script src="./_content/MudBlazor/MudBlazor.min.js"></script>`  
**Line 28**: `<script src="./_content/CodeBeam.MudBlazor.Extensions/MudExtensions.min.js"></script>`

### React Wrapper Storybook
```
src/wrappers/wrappers/react-wrapper/.storybook/preview-head.html
src/wrappers/wrappers/react-wrapper/.storybook/preview-body.html
```
**CSS**: preview-head.html  
**JS**: preview-body.html

---

## 4. Service Layer Files (C#)

### Core Interfaces

#### IThemeService
```
src/framework/Elsa.Studio.Core/Contracts/IThemeService.cs
```
**Line 1**: `using MudBlazor;`  
**Dependency**: Returns `MudTheme` and `Palette` objects  
**References**: 64+ files

#### IUserMessageService
```
src/framework/Elsa.Studio.Core/Contracts/IUserMessageService.cs
```
**Line 1**: `using MudBlazor;`  
**Dependency**: Uses `Severity` and `SnackbarOptions` enums  
**References**: 64+ files (via ISnackbar)

### Core Implementations

#### DefaultThemeService
```
src/framework/Elsa.Studio.Core/Services/DefaultThemeService.cs
```
**Lines 2-3**: 
```csharp
using MudBlazor;
using MudBlazor.Utilities;
```
**Creates**: Custom MudTheme with PaletteLight and PaletteDark

#### DefaultUserMessageService
```
src/framework/Elsa.Studio.Core/Services/DefaultUserMessageService.cs
```
**Line 2**: `using MudBlazor;`  
**Injects**: `ISnackbar` service

#### UnsupportedUIHintHandler
```
src/framework/Elsa.Studio.Core/Services/UnsupportedUIHintHandler.cs
```
**Line 4**: `using MudBlazor;`  
**Usage**: Limited

### Shell Extensions

#### ServiceCollectionExtensions
```
src/framework/Elsa.Studio.Shell/Extensions/ServiceCollectionExtensions.cs
```
**Lines 5-7**:
```csharp
using MudBlazor;
using MudBlazor.Services;
using MudExtensions.Services;
```
**Registers**: 
- `.AddMudServices()` with Snackbar configuration
- `.AddMudExtensions()`

---

## 5. Layout Files

### MainLayout
```
src/framework/Elsa.Studio.Shared/Layouts/MainLayout.razor
src/framework/Elsa.Studio.Shared/Layouts/MainLayout.razor.cs
```
**Components Used**:
- MudThemeProvider
- MudPopoverProvider
- MudDialogProvider
- MudSnackbarProvider
- MudLayout
- MudAppBar
- MudIconButton
- MudSpacer
- MudDrawer
- MudDrawerHeader
- MudMainContent

**C# Dependencies**:
- IThemeService (CurrentTheme, IsDarkMode)
- IDialogService
- Uses MudTheme object

### BasicLayout
```
src/framework/Elsa.Studio.Shared/Layouts/BasicLayout.razor
```
**Components Used**:
- MudThemeProvider
- MudSnackbarProvider
- MudPopoverProvider
- MudDialogProvider

**Note**: Also uses Radzen components (hybrid layout)

---

## 6. Shared Components (Framework)

### Navigation Components

#### NavMenuItem
```
src/framework/Elsa.Studio.Shared/Components/NavMenuItem.razor
```
**Components**: MudNavLink, MudNavGroup

### AppBar Components

#### DarkModeToggle
```
src/framework/Elsa.Studio.Shared/Components/AppBar/DarkModeToggle.razor
```
**Components**: MudToggleIconButton  
**Icons**: Icons.Material.Outlined.LightMode, Icons.Material.Outlined.DarkMode

#### ProductInfo
```
src/framework/Elsa.Studio.Shared/Components/AppBar/ProductInfo.razor
```
**Components**: MudIconButton  
**Icons**: Icons.Material.Outlined.Info

#### GitHub
```
src/framework/Elsa.Studio.Shared/Components/AppBar/GitHub.razor
```
**Components**: MudIconButton  
**Icons**: Icons.Custom.Brands.GitHub

#### Documentation
```
src/framework/Elsa.Studio.Shared/Components/AppBar/Documentation.razor
```
**Components**: MudIconButton  
**Icons**: Icons.Material.Outlined.Book

### Dialog Components

#### CodeEditorDialog
```
src/framework/Elsa.Studio.Shared/Components/CodeEditorDialog.razor
src/framework/Elsa.Studio.Shared/Components/CodeEditorDialog.razor.cs
```
**Components**: MudDialog, MudToolBar, MudStack, MudText, MudSpacer, MudTooltip, MudIconButton, MudPaper  
**C#**: IDialogService usage

#### MarkdownEditor
```
src/framework/Elsa.Studio.Shared/Components/MarkdownEditor.razor
```
**Components**: MudDialog, MudToolBar, MudStack, MudText, MudIconButton

### Input Components

#### ExpressionEditor
```
src/framework/Elsa.Studio.Shared/Components/ExpressionEditor.razor
src/framework/Elsa.Studio.Shared/Components/ExpressionEditor.razor.cs
```
**Components**: MudStack, MudField, MudMenu, MudMenuItem, MudDivider, MudIconButton  
**C#**: IDialogService, Icons.Material.*

#### ExpressionInput
```
src/framework/Elsa.Studio.Shared/Components/ExpressionInput.razor
src/framework/Elsa.Studio.Shared/Components/ExpressionInput.razor.cs
```
**Components**: MudStack, MudTextField, MudMenu, MudMenuItem, MudDivider, MudIconButton  
**C#**: IDialogService, Icons.Material.*

### Display Components

#### DataPanel
```
src/framework/Elsa.Studio.Shared/Components/DataPanel.razor
```
**Components**: MudText, MudSimpleTable, MudStack, MudTooltip, MudIconButton, MudLink, MudAlert

#### ContentVisualizer
```
src/framework/Elsa.Studio.Shared/Components/ContentVisualizer.razor
src/framework/Elsa.Studio.Shared/Components/ContentVisualizer.razor.cs
```
**Components**: MudDialog, MudToolBar, MudStack, MudIconButton, MudToggleIconButton, MudPaper  
**C#**: IDialogService

#### ClientPackageVersion
```
src/framework/Elsa.Studio.Shared/Components/ClientPackageVersion.razor
```
**Components**: MudText

#### FieldExtension
```
src/framework/Elsa.Studio.Shared/Components/FieldExtension.razor
```
**Components**: MudStack

#### Unauthorized
```
src/framework/Elsa.Studio.Shared/Components/Unauthorized.razor
```
**Components**: MudAlert  
**Icons**: Icons.Material.Outlined.Announcement

---

## 7. Shell Framework

### App.razor
```
src/framework/Elsa.Studio.Shell/App.razor
```
**Components**: MudText, MudAlert

---

## 8. Workflows Module (40+ files)

### List Components

#### WorkflowDefinitionList
```
src/modules/Elsa.Studio.Workflows/Components/WorkflowDefinitionList/WorkflowDefinitionList.razor
src/modules/Elsa.Studio.Workflows/Components/WorkflowDefinitionList/WorkflowDefinitionList.razor.cs
```
**Components**: MudAlert, MudContainer, MudFileUpload, **MudTable**, MudMenu, MudMenuItem, MudDivider, MudSpacer, MudTextField, MudButtonGroup, MudButton, MudTooltip, MudTh, MudTableSortLabel, MudTd, MudIconButton, MudChip  
**C#**: IDialogService, QueryTableComponentBase (uses MudBlazor)  
**Complexity**: HIGH (complex table with server data)

#### WorkflowInstanceList
```
src/modules/Elsa.Studio.Workflows/Components/WorkflowInstanceList/WorkflowInstanceList.razor
src/modules/Elsa.Studio.Workflows/Components/WorkflowInstanceList/WorkflowInstanceList.razor.cs
```
**Components**: **MudTable**, MudMenu, MudMenuItem, MudDivider, MudSpacer, MudTextField, MudButtonGroup, MudButton, MudTooltip, MudTh, MudTableSortLabel, MudTd, MudIconButton, MudChip  
**C#**: IDialogService, QueryTableComponentBase  
**Complexity**: HIGH

#### BulkCancelDialog
```
src/modules/Elsa.Studio.Workflows/Components/WorkflowInstanceList/Components/BulkCancelDialog.razor
```
**Components**: Dialog components

### Dialog Components

#### CreateWorkflowDialog
```
src/modules/Elsa.Studio.Workflows/Components/WorkflowDefinitionList/CreateWorkflowDialog.razor
src/modules/Elsa.Studio.Workflows/Components/WorkflowDefinitionList/CreateWorkflowDialog.razor.cs
```
**Components**: MudDialog, MudTextField, MudSelect, MudSelectItem, MudButton  
**C#**: IDialogService

#### CloneWorkflowDialog
```
src/modules/Elsa.Studio.Workflows/Components/WorkflowDefinitionList/CloneWorkflowDialog.razor
src/modules/Elsa.Studio.Workflows/Components/WorkflowDefinitionList/CloneWorkflowDialog.razor.cs
```
**Components**: MudDialog, MudTextField, MudButton  
**C#**: IDialogService

### Editor Components

#### WorkflowDefinitionEditor
```
src/modules/Elsa.Studio.Workflows/Components/WorkflowDefinitionEditor/WorkflowDefinitionEditor.razor
```
**Components**: MudButton

#### WorkflowEditor
```
src/modules/Elsa.Studio.Workflows/Components/WorkflowDefinitionEditor/Components/WorkflowEditor.razor
src/modules/Elsa.Studio.Workflows/Components/WorkflowDefinitionEditor/Components/WorkflowEditor.razor.cs
```
**Components**: Many MudBlazor components  
**C#**: IDialogService, WorkflowEditorComponentBase (uses MudBlazor)

#### WorkflowDefinitionWorkspace
```
src/modules/Elsa.Studio.Workflows/Components/WorkflowDefinitionEditor/Components/WorkflowDefinitionWorkspace.razor
src/modules/Elsa.Studio.Workflows/Components/WorkflowDefinitionEditor/Components/WorkflowDefinitionWorkspace.razor.cs
```
**C#**: Uses MudBlazor enums

#### CodeView
```
src/modules/Elsa.Studio.Workflows/Components/WorkflowDefinitionEditor/Components/CodeView.razor
src/modules/Elsa.Studio.Workflows/Components/WorkflowDefinitionEditor/Components/CodeView.razor.cs
```
**Inline Import**: `@using MudBlazor`  
**C#**: IDialogService

#### WorkflowDefinitionVersionViewer
```
src/modules/Elsa.Studio.Workflows/Components/WorkflowDefinitionEditor/Components/WorkflowDefinitionVersionViewer.razor
```
**Components**: MudAlert

### Activity Properties Panel

#### ActivityPropertiesPanel
```
src/modules/Elsa.Studio.Workflows/Components/WorkflowDefinitionEditor/Components/ActivityProperties/ActivityPropertiesPanel.razor
src/modules/Elsa.Studio.Workflows/Components/WorkflowDefinitionEditor/Components/ActivityProperties/ActivityPropertiesPanel.razor.cs
```
**Components**: MudTabs, MudTabPanel  
**C#**: IDialogService

#### Property Tabs (Multiple files)
```
src/modules/Elsa.Studio.Workflows/Components/WorkflowDefinitionEditor/Components/ActivityProperties/Tabs/
```
- CommonTab.razor
- CommitStrategyTab.razor
- LogPersistenceTab.razor
- ResilienceTab.razor
- TaskTab.razor
- VersionTab.razor
- Tests/TestTab.razor
- Outputs/Components/OutputsTab.razor

All use MudBlazor components (MudTextField, MudSelect, etc.)

### Workflow Properties

#### InputOutput Tab
```
src/modules/Elsa.Studio.Workflows/Components/WorkflowDefinitionEditor/Components/WorkflowProperties/Tabs/InputOutput/Components/
```

**InputsSection.razor + .cs**:
- Components: MudTextField, MudSelect, MudButton, MudIconButton, MudTable, MudTh, MudTd
- C#: IDialogService

**EditInputDialog.razor + .cs**:
- Components: MudDialog, MudTextField, MudCheckBox, MudSelect, MudButton
- C#: IDialogService

**OutputsSection.razor + .cs**:
- Similar to InputsSection

**EditOutputDialog.razor + .cs**:
- Similar to EditInputDialog

**OutcomesSection.razor + .cs**:
- Uses MudExtensions (DynamicOutcomes)

#### Variables Tab
```
src/modules/Elsa.Studio.Workflows/Components/WorkflowDefinitionEditor/Components/WorkflowProperties/Tabs/Variables/Components/
```

**VariablesTab.razor + .cs**:
- Components: MudTable, MudTh, MudTd, MudIconButton, MudButton
- C#: IDialogService

**EditVariableDialog.razor + .cs**:
- Components: MudDialog, MudTextField, MudCheckBox, MudButton
- C#: IDialogService

**EditVariableTestValueDialog.razor + .cs**:
- Components: MudDialog, MudTextField, MudButton
- C#: IDialogService

#### Properties Tab
```
src/modules/Elsa.Studio.Workflows/Components/WorkflowDefinitionEditor/Components/WorkflowProperties/Tabs/Properties/Sections/
```

**Settings.razor**: MudTextField, MudCheckBox  
**Metadata.razor**: MudTextField, MudButton, MudIconButton

#### Version History Tab
```
src/modules/Elsa.Studio.Workflows/Components/WorkflowDefinitionEditor/Components/WorkflowProperties/Tabs/VersionHistory/
```

**VersionHistoryTab.razor + .cs**:
- Components: MudTable, MudTh, MudTd, MudIconButton
- C#: IDialogService

### Workflow Instance Components

#### WorkflowInstanceViewer
```
src/modules/Elsa.Studio.Workflows/Components/WorkflowInstanceViewer/WorkflowInstanceViewer.razor
```

#### WorkflowInstanceWorkspace
```
src/modules/Elsa.Studio.Workflows/Components/WorkflowInstanceViewer/Components/WorkflowInstanceWorkspace.razor
src/modules/Elsa.Studio.Workflows/Components/WorkflowInstanceViewer/Components/WorkflowInstanceWorkspace.razor.cs
```
**C#**: Uses MudBlazor enums

#### WorkflowInstanceDesigner
```
src/modules/Elsa.Studio.Workflows/Components/WorkflowInstanceViewer/Components/WorkflowInstanceDesigner.razor.cs
```
**C#**: Uses MudBlazor Color enum for activity highlighting

#### WorkflowInstanceDetails
```
src/modules/Elsa.Studio.Workflows/Components/WorkflowInstanceViewer/Components/WorkflowInstanceDetails.razor
```
**Components**: MudTabs, MudTabPanel

#### ActivityExecutionsTab
```
src/modules/Elsa.Studio.Workflows/Components/WorkflowInstanceViewer/Components/ActivityExecutionsTab.razor.cs
```
**C#**: MudBlazor Color enum

#### RetriesTab
```
src/modules/Elsa.Studio.Workflows/Components/WorkflowInstanceViewer/Components/RetriesTab.razor.cs
```
**C#**: MudBlazor Color enum

#### Journal
```
src/modules/Elsa.Studio.Workflows/Components/WorkflowInstanceViewer/Components/Journal.razor
```
**Components**: MudTabs, MudTabPanel

#### Other Instance Components
- ActivityDetailsTab.razor
- JournalEntryDetailsTab.razor
- ElapsedTime.razor

### Activity Pickers

#### Accordion Activity Picker
```
src/modules/Elsa.Studio.Workflows/ActivityPickers/Accordion/ActivityPicker.razor
```
**Components**: MudAccordion, MudAccordionItem (if exists)

#### Treeview Activity Picker
```
src/modules/Elsa.Studio.Workflows/ActivityPickers/Treeview/ActivityPicker.razor
src/modules/Elsa.Studio.Workflows/ActivityPickers/Treeview/ActivityPicker.razor.cs
src/modules/Elsa.Studio.Workflows/ActivityPickers/Treeview/ActivityTreeItem.cs
```
**Components**: MudTreeView, MudTreeViewItem (if exists)  
**C#**: Uses MudBlazor Color enum for tree items

### Shared Workflow Components

#### DiagramDesignerWrapper
```
src/modules/Elsa.Studio.Workflows/Shared/Components/DiagramDesignerWrapper.razor.cs
```
**C#**: Uses MudBlazor types

### Base Classes

#### QueryTableComponentBase
```
src/modules/Elsa.Studio.Workflows/Components/QueryTableComponentBase.cs
```
**C#**: Base class for table components, uses MudBlazor MudTable types

#### WorkflowEditorComponentBase
```
src/modules/Elsa.Studio.Workflows/Components/WorkflowDefinitionEditor/Components/WorkflowEditorComponentBase.cs
```
**C#**: Uses MudBlazor IDialogService

### Services

#### WorkflowCloningDialogService
```
src/modules/Elsa.Studio.Workflows/Services/WorkflowCloningDialogService.cs
```
**C#**: IDialogService, DialogOptions, DialogParameters

### Menu Providers

#### WorkflowsMenu
```
src/modules/Elsa.Studio.Workflows/Menu/WorkflowsMenu.cs
```
**C#**: Uses MudBlazor Icons

### Diagram Designers

#### FlowchartDiagramDesigner
```
src/modules/Elsa.Studio.Workflows/DiagramDesigners/Flowcharts/FlowchartDiagramDesigner.cs
```
**C#**: Uses MudBlazor Icons

---

## 9. Workflows Core Module

### Activity Display Settings
```
src/modules/Elsa.Studio.Workflows.Core/UI/Providers/DefaultActivityDisplaySettingsProvider.cs
```
**C#**: Maps activities to MudBlazor Icons (Icons.Material.*)  
**50+ activity mappings**

---

## 10. Workflows Designer Module

### FlowchartDesigner
```
src/modules/Elsa.Studio.Workflows.Designer/Components/FlowchartDesigner.razor.cs
```
**C#**: Uses MudBlazor types

### Activity Wrappers
```
src/modules/Elsa.Studio.Workflows.Designer/Components/ActivityWrappers/V1/ActivityWrapper.razor
src/modules/Elsa.Studio.Workflows.Designer/Components/ActivityWrappers/V2/ActivityWrapper.razor
```
**Components**: Activity visualization in designer

---

## 11. UIHints Module (20+ files)

### MudExtensions Components (7 files)

#### Dropdown
```
src/modules/Elsa.Studio.UIHints/Components/Dropdown.razor
```
**Components**: **MudSelectExtended**, MudSelectItemExtended

#### OutcomePicker
```
src/modules/Elsa.Studio.UIHints/Components/OutcomePicker.razor
```
**Components**: **MudSelectExtended**, MudSelectItemExtended

#### VariablePicker
```
src/modules/Elsa.Studio.UIHints/Components/VariablePicker.razor
```
**Components**: **MudSelectExtended**, MudSelectItemExtended

#### InputPicker
```
src/modules/Elsa.Studio.UIHints/Components/InputPicker.razor
```
**Components**: **MudSelectExtended**, MudSelectItemExtended

#### OutputPicker
```
src/modules/Elsa.Studio.UIHints/Components/OutputPicker.razor
```
**Components**: **MudSelectExtended**, MudSelectItemExtended

#### TypePicker
```
src/modules/Elsa.Studio.UIHints/Components/TypePicker.razor
```
**Components**: **MudSelectExtended**, MudSelectItemExtended

#### HttpStatusCodes
```
src/modules/Elsa.Studio.UIHints/Components/HttpStatusCodes.razor.cs
```
**C#**: Uses MudExtensions

#### MultiText
```
src/modules/Elsa.Studio.UIHints/Components/MultiText.razor.cs
```
**C#**: Uses MudExtensions

#### DynamicOutcomes
```
src/modules/Elsa.Studio.UIHints/Components/DynamicOutcomes.razor.cs
```
**C#**: Uses MudExtensions

### Standard Input Components

#### Cases
```
src/modules/Elsa.Studio.UIHints/Components/Cases.razor
src/modules/Elsa.Studio.UIHints/Components/Cases.razor.cs
```
**Components**: MudTable, MudTh, MudTd, MudIconButton, MudButton  
**Icons**: Icons.Material.Filled.Delete, Icons.Material.Outlined.Add

#### Code
```
src/modules/Elsa.Studio.UIHints/Components/Code.razor
src/modules/Elsa.Studio.UIHints/Components/Code.razor.cs
```
**Components**: MudIconButton  
**C#**: IDialogService  
**Icons**: Icons.Material.Outlined.EditNote

#### Dictionary
```
src/modules/Elsa.Studio.UIHints/Components/Dictionary.razor
src/modules/Elsa.Studio.UIHints/Components/Dictionary.razor.cs
```
**Components**: MudTable, MudTh, MudTd, MudIconButton, MudButton  
**C#**: IDialogService  
**Icons**: Icons.Material.Filled.Delete, Icons.Material.Outlined.Add

#### SingleLine
```
src/modules/Elsa.Studio.UIHints/Components/SingleLine.razor
```
**Components**: MudTextField, MudIconButton  
**Icons**: Icons.Material.Filled.ContentCopy

#### DateTimePicker
```
src/modules/Elsa.Studio.UIHints/Components/DateTimePicker.razor
```
**Components**: MudDatePicker, MudTimePicker, MudToggleIconButton  
**Icons**: Icons.Material.Filled.AccessTime, Icons.Material.Filled.Close

#### Other UIHint Components
- CheckBox.razor
- CheckList.razor
- CodeEditor.razor
- DropDown.razor
- MultiLine.razor
- Radio.razor
- Switch.razor
- TextArea.razor
And more...

---

## 12. Login Module

### Login Page
```
src/modules/Elsa.Studio.Login/Pages/Login/Login.razor
src/modules/Elsa.Studio.Login/Pages/Login/Login.razor.cs
```
**C#**: Uses MudBlazor types

---

## 13. Dashboard Module

### Menu Provider
```
src/modules/Elsa.Studio.Dashboard/Menu/DashboardMenu.cs
```
**C#**: Uses MudBlazor Icons

---

## 14. Security Module

### Menu Provider
```
src/modules/Elsa.Studio.Security/Menu/SecurityMenu.cs
```
**C#**: Uses MudBlazor Icons

---

## 15. Counter Module

### Menu Provider
```
src/modules/Elsa.Studio.Counter/Menu/CounterMenu.cs
```
**C#**: Uses MudBlazor Icons

---

## 16. Environments Module

### Environment Picker
```
src/modules/Elsa.Studio.Environments/Components/EnvironmentPicker.razor
```
**Inline Import**: `@using MudBlazor` (no _Imports.razor)  
**Components**: MudSelect, MudSelectItem

---

## 17. Localization Module

### Extensions
```
src/modules/Elsa.Studio.Localization/Extensions/ServiceCollectionExtensions.cs
```
**Note**: References MudBlazor.Translations package

---

## 18. Sample Applications

### BlazorApp1
```
samples/BlazorApp1/Components/Pages/WorkflowDefinitionEditor.razor
```
**Components**: MudButton

---

## Migration Assignment Checklist

### Phase 1: Foundation (Weeks 1-2)
- [ ] IThemeService abstraction
- [ ] IDialogService abstraction
- [ ] IUserMessageService abstraction
- [ ] ServiceCollectionExtensions updates

### Phase 2: Layout (Weeks 3-4)
- [ ] MainLayout.razor + .cs
- [ ] BasicLayout.razor
- [ ] NavMenuItem.razor
- [ ] All AppBar components (4 files)

### Phase 3: Shared Components (Weeks 5-7)
- [ ] ExpressionEditor.razor + .cs
- [ ] ExpressionInput.razor + .cs
- [ ] CodeEditorDialog.razor + .cs
- [ ] DataPanel.razor
- [ ] ContentVisualizer.razor + .cs
- [ ] MarkdownEditor.razor
- [ ] Other shared components (5 files)

### Phase 4: Dialogs (Weeks 8-9)
- [ ] CreateWorkflowDialog.razor + .cs
- [ ] CloneWorkflowDialog.razor + .cs
- [ ] EditInputDialog.razor + .cs
- [ ] EditOutputDialog.razor + .cs
- [ ] EditVariableDialog.razor + .cs
- [ ] EditVariableTestValueDialog.razor + .cs
- [ ] BulkCancelDialog.razor
- [ ] All dialog service calls (30+ files)

### Phase 5: Tables (Weeks 10-12)
- [ ] QueryTableComponentBase.cs
- [ ] WorkflowDefinitionList.razor + .cs (CRITICAL)
- [ ] WorkflowInstanceList.razor + .cs (CRITICAL)
- [ ] ActivityExecutionsTab.razor.cs
- [ ] RetriesTab.razor.cs
- [ ] VersionHistoryTab.razor + .cs
- [ ] All MudTable usages (8 files)

### Phase 6: UIHints (Weeks 13-15)
- [ ] Dropdown.razor (MudExtensions)
- [ ] OutcomePicker.razor (MudExtensions)
- [ ] VariablePicker.razor (MudExtensions)
- [ ] InputPicker.razor (MudExtensions)
- [ ] OutputPicker.razor (MudExtensions)
- [ ] TypePicker.razor (MudExtensions)
- [ ] HttpStatusCodes.razor.cs (MudExtensions)
- [ ] MultiText.razor.cs (MudExtensions)
- [ ] DynamicOutcomes.razor.cs (MudExtensions)
- [ ] Cases.razor + .cs
- [ ] Code.razor + .cs
- [ ] Dictionary.razor + .cs
- [ ] DateTimePicker.razor
- [ ] SingleLine.razor
- [ ] All other UIHint components (20+ files)

### Phase 7: Workflow Editor (Weeks 16-18)
- [ ] WorkflowEditor.razor + .cs
- [ ] WorkflowEditorComponentBase.cs
- [ ] WorkflowDefinitionWorkspace.razor + .cs
- [ ] ActivityPropertiesPanel.razor + .cs
- [ ] All property tabs (15+ files)
- [ ] WorkflowProperties tabs (10+ files)
- [ ] WorkflowInstanceViewer components (10+ files)
- [ ] CodeView.razor + .cs

### Phase 8: Modules (Weeks 19-20)
- [ ] Login/Login.razor + .cs
- [ ] Dashboard pages
- [ ] Security pages
- [ ] Counter pages
- [ ] Environments/EnvironmentPicker.razor
- [ ] All menu providers (4 files)
- [ ] Activity pickers (3 files)
- [ ] FlowchartDesigner.razor.cs
- [ ] ActivityWrapper.razor (V1, V2)

### Phase 9: Cleanup (Weeks 21-22)
- [ ] Remove MudBlazor from Directory.Packages.props
- [ ] Remove from Elsa.Studio.Core.csproj
- [ ] Remove from Elsa.Studio.Localization.csproj
- [ ] Update all _Imports.razor files (14 files)
- [ ] Update all host HTML files (6 files)
- [ ] Update loading splash CSS
- [ ] Update DefaultActivityDisplaySettingsProvider.cs icons
- [ ] Remove MudExtensions service registration
- [ ] Final testing and validation

---

## Quick Stats Summary

**By File Type**:
- C# Files: 49
- Razor Components: 80+
- _Imports.razor: 14
- HTML/CSHTML: 6
- Total: 143+ files

**By Module**:
- Framework (Shared + Shell + Core): 20 files
- Workflows: 40+ files
- UIHints: 20+ files
- Workflows.Designer: 3 files
- Workflows.Core: 1 file
- Login: 1 file
- Dashboard: 1+ files
- Security: 1+ files
- Counter: 1+ files
- Localization: 1 file
- Environments: 1 file
- Hosts: 6 files
- Samples: 2 files

**By Complexity**:
- High Complexity: 25 files (Tables, Dialogs, Services, Layout)
- Medium Complexity: 50 files (Tabs, Menus, Forms, MudExtensions)
- Low Complexity: 68 files (Simple components, text, icons, buttons)

---

**Document Version**: 1.0  
**Last Updated**: 2026-01-06  
**Status**: Complete
