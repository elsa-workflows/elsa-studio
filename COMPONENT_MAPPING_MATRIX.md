# MudBlazor to Radzen Component Mapping Matrix

> **Purpose**: Detailed mapping of MudBlazor components to Radzen equivalents with migration strategies  
> **Version**: 1.0  
> **Date**: 2026-01-06

---

## Table of Contents
1. [Layout Components](#layout-components)
2. [Typography & Display](#typography--display)
3. [Input Components](#input-components)
4. [Table Components](#table-components)
5. [Navigation Components](#navigation-components)
6. [Feedback Components](#feedback-components)
7. [Service Layer](#service-layer)
8. [Theming & Styling](#theming--styling)

---

## Layout Components

### MudLayout → RadzenLayout
**Usage**: 1 occurrence (MainLayout.razor)

**MudBlazor**:
```razor
<MudLayout>
    <MudAppBar>...</MudAppBar>
    <MudDrawer>...</MudDrawer>
    <MudMainContent>@Body</MudMainContent>
</MudLayout>
```

**Radzen Equivalent**:
```razor
<RadzenLayout>
    <RadzenHeader>...</RadzenHeader>
    <RadzenSidebar>...</RadzenSidebar>
    <RadzenBody>@Body</RadzenBody>
</RadzenLayout>
```

**Migration Complexity**: Medium
- Different component structure
- Need to adjust nested component placement
- Event handling may differ

**Notes**: 
- Radzen uses separate Header/Sidebar/Body components instead of unified Layout
- May need custom CSS for exact layout match

---

### MudAppBar → RadzenHeader
**Usage**: 1 occurrence (MainLayout.razor)

**MudBlazor**:
```razor
<MudAppBar Elevation="0" Color="Color.Inherit">
    <MudIconButton Icon="@Icons.Material.Filled.Menu" OnClick="@DrawerToggle"/>
    <MudSpacer/>
    <!-- AppBar items -->
</MudAppBar>
```

**Radzen Equivalent**:
```razor
<RadzenHeader>
    <RadzenButton Icon="menu" ButtonStyle="ButtonStyle.Light" Click="@DrawerToggle"/>
    <div style="flex: 1"></div>
    <!-- Header items -->
</RadzenHeader>
```

**Migration Complexity**: Medium
- Icon system different
- Color/elevation props different
- MudSpacer needs CSS replacement

**Notes**:
- Radzen Header is less feature-rich
- May need custom styling for exact appearance

---

### MudDrawer → RadzenSidebar
**Usage**: 1 occurrence (MainLayout.razor)

**MudBlazor**:
```razor
<MudDrawer @bind-Open="_drawerOpen" 
           Elevation="0" 
           ClipMode="DrawerClipMode.Always">
    <MudDrawerHeader>
        @BrandingProvider.Branding
    </MudDrawerHeader>
    <NavMenu/>
</MudDrawer>
```

**Radzen Equivalent**:
```razor
<RadzenSidebar @bind-Expanded="_drawerOpen">
    <RadzenPanelMenu>
        <RadzenPanelMenuItem Text="Header">
            @BrandingProvider.Branding
        </RadzenPanelMenuItem>
        <NavMenu/>
    </RadzenPanelMenu>
</RadzenSidebar>
```

**Migration Complexity**: Medium
- State binding similar
- ClipMode not directly available
- DrawerHeader needs restructuring

**Notes**:
- Radzen Sidebar has different styling defaults
- May need CSS for overlay behavior

---

### MudContainer → RadzenRow + RadzenColumn
**Usage**: 7 occurrences

**MudBlazor**:
```razor
<MudContainer MaxWidth="MaxWidth.False">
    <PageHeading Text="@Title" />
    <MudTable>...</MudTable>
</MudContainer>
```

**Radzen Equivalent**:
```razor
<RadzenRow>
    <RadzenColumn>
        <PageHeading Text="@Title" />
        <RadzenDataGrid>...</RadzenDataGrid>
    </RadzenColumn>
</RadzenRow>
```

**Migration Complexity**: Low
- Simple replacement
- Radzen uses Bootstrap-like grid system
- May need to adjust column sizing

**Notes**:
- MaxWidth.False → full width column
- Radzen grid is responsive by default

---

### MudPaper → RadzenCard
**Usage**: 15 occurrences

**MudBlazor**:
```razor
<MudPaper Height="655px" Width="100%" Elevation="0">
    <!-- Content -->
</MudPaper>
```

**Radzen Equivalent**:
```razor
<RadzenCard Style="height: 655px; width: 100%">
    <!-- Content -->
</RadzenCard>
```

**Migration Complexity**: Low
- Direct replacement
- Elevation → drop shadow CSS
- Height/Width as inline styles

**Notes**:
- RadzenCard has built-in padding
- May need custom CSS for exact match

---

### MudStack → RadzenStack
**Usage**: 69 occurrences (most used layout component!)

**MudBlazor**:
```razor
<MudStack Row="true" Spacing="1" AlignItems="AlignItems.Center">
    <MudText>Item 1</MudText>
    <MudText>Item 2</MudText>
</MudStack>
```

**Radzen Equivalent**:
```razor
<RadzenStack Orientation="Orientation.Horizontal" Gap="0.25rem" AlignItems="AlignItems.Center">
    <RadzenText>Item 1</RadzenText>
    <RadzenText>Item 2</RadzenText>
</RadzenStack>
```

**Migration Complexity**: Low
- Very similar API
- Row → Orientation.Horizontal
- Spacing → Gap (convert units)

**Notes**:
- Radzen Stack added in recent versions
- Nearly identical functionality

---

## Typography & Display

### MudText → RadzenText or Native HTML
**Usage**: 54 occurrences

**MudBlazor**:
```razor
<MudText Typo="Typo.h5">Heading</MudText>
<MudText Typo="Typo.body2" Color="@Color.Info">Body text</MudText>
<MudText Typo="Typo.caption">Caption</MudText>
```

**Radzen Equivalent**:
```razor
<RadzenText TextStyle="TextStyle.H5">Heading</RadzenText>
<RadzenText TextStyle="TextStyle.Body2" Style="color: var(--rz-info)">Body text</RadzenText>
<RadzenText TextStyle="TextStyle.Caption">Caption</RadzenText>
```

**Alternative (Native HTML)**:
```razor
<h5>Heading</h5>
<p class="rz-body2 rz-color-info">Body text</p>
<small>Caption</small>
```

**Migration Complexity**: Low
- Typo → TextStyle mapping straightforward
- Color may need CSS variables
- Consider native HTML for simplicity

**Typography Mapping**:
| MudBlazor Typo | Radzen TextStyle | Native HTML |
|----------------|------------------|-------------|
| h1 | H1 | `<h1>` |
| h2 | H2 | `<h2>` |
| h3 | H3 | `<h3>` |
| h4 | H4 | `<h4>` |
| h5 | H5 | `<h5>` |
| h6 | H6 | `<h6>` |
| body1 | Body1 | `<p>` |
| body2 | Body2 | `<p class="small">` |
| subtitle1 | Subtitle1 | `<p class="lead">` |
| subtitle2 | Subtitle2 | `<p class="lead small">` |
| caption | Caption | `<small>` |
| overline | Overline | `<span class="text-uppercase">` |

---

### MudIcon → RadzenIcon
**Usage**: 11 occurrences

**MudBlazor**:
```razor
<MudIcon Icon="@Icons.Material.Filled.Check" />
<MudIcon Icon="@Icons.Material.Outlined.Close" Color="Color.Primary" Size="Size.Large" />
```

**Radzen Equivalent**:
```razor
<RadzenIcon Icon="check" />
<RadzenIcon Icon="close" IconColor="Colors.Primary" IconStyle="IconStyle.Large" />
```

**Migration Complexity**: Low
- Different icon naming convention
- Material Icons supported in Radzen
- Size/Color props slightly different

**Icon System Notes**:
- MudBlazor: `Icons.Material.Filled.Check`
- Radzen: String-based "check" (Material Icons)
- Need icon name mapping dictionary

---

## Input Components

### MudTextField → RadzenTextBox
**Usage**: 30 occurrences

**MudBlazor**:
```razor
<MudTextField @bind-Value="_model.Name"
              Label="Name"
              Required="true"
              Variant="Variant.Outlined"
              Margin="Margin.Dense"
              HelperText="Enter your name"
              Adornment="Adornment.End"
              AdornmentIcon="@Icons.Material.Outlined.Search" />
```

**Radzen Equivalent**:
```razor
<RadzenTextBox @bind-Value="_model.Name"
               Placeholder="Name"
               Style="width: 100%"
               aria-label="Name" />
<RadzenRequiredValidator Component="nameInput" Text="Name is required" />
```

**Migration Complexity**: Low-Medium
- Core binding identical
- Label → Placeholder (or separate RadzenLabel)
- Variant → Style prop
- Adornments need custom implementation
- Validation separate component

**Notes**:
- Radzen uses separate validator components
- Adornments require custom CSS/HTML
- Dense variant → custom styling

---

### MudSelect → RadzenDropDown
**Usage**: 18 occurrences (regular), 13 occurrences (Extended)

**MudBlazor**:
```razor
<MudSelect @bind-Value="_model.Type" Label="Type" Variant="Variant.Outlined">
    <MudSelectItem Value="@TypeA">Type A</MudSelectItem>
    <MudSelectItem Value="@TypeB">Type B</MudSelectItem>
</MudSelect>
```

**Radzen Equivalent**:
```razor
<RadzenDropDown @bind-Value="_model.Type" 
                Data="@types" 
                TextProperty="Name" 
                ValueProperty="Value"
                Placeholder="Type"
                Style="width: 100%" />
```

**Migration Complexity**: Medium
- Different data binding approach
- Radzen uses Data collection instead of child items
- Need to convert items to collection
- Multi-select → RadzenDropDown with Multiple="true"

**Notes**:
- Radzen more data-driven
- Better performance with large datasets
- Built-in search in AllowFiltering="true"

---

### MudSelectExtended → RadzenDropDown (Enhanced)
**Usage**: 13 occurrences (MudExtensions)

**MudBlazor (MudExtensions)**:
```razor
<MudSelectExtended T="SelectListItem"
                   @bind-Value="_selectedItem"
                   Label="Select Item"
                   SearchBox="true"
                   SearchFunc="@SearchItems"
                   Virtualize="true">
    @foreach (var item in _items)
    {
        <MudSelectItemExtended Value="@item">@item.Text</MudSelectItemExtended>
    }
</MudSelectExtended>
```

**Radzen Equivalent**:
```razor
<RadzenDropDown @bind-Value="_selectedItem"
                Data="@_items"
                TextProperty="Text"
                ValueProperty="Value"
                AllowFiltering="true"
                FilterCaseSensitivity="FilterCaseSensitivity.CaseInsensitive"
                AllowVirtualization="true"
                Placeholder="Select Item"
                Style="width: 100%" />
```

**Migration Complexity**: Medium-High
- MudExtensions provides enhanced select with search, virtualization
- Radzen has similar features built-in
- SearchFunc → FilterOperator or custom filter
- May need custom implementation for complex scenarios

**Feature Comparison**:
| Feature | MudSelectExtended | RadzenDropDown |
|---------|-------------------|----------------|
| Search | ✅ SearchBox + SearchFunc | ✅ AllowFiltering |
| Virtualization | ✅ Virtualize | ✅ AllowVirtualization |
| Multi-select | ✅ MultiSelection | ✅ Multiple |
| Custom template | ✅ ItemTemplate | ✅ Template |
| Loading state | ✅ | ✅ LoadData event |

---

### MudCheckBox → RadzenCheckBox
**Usage**: 11 occurrences

**MudBlazor**:
```razor
<MudCheckBox @bind-Checked="_model.IsActive" Label="Active" Color="Color.Primary" />
```

**Radzen Equivalent**:
```razor
<RadzenCheckBox @bind-Value="_model.IsActive" Name="active" />
<RadzenLabel Text="Active" Component="active" />
```

**Migration Complexity**: Low
- Checked → Value
- Label separate component in Radzen
- Color not directly available

---

### MudButton → RadzenButton
**Usage**: 32 occurrences

**MudBlazor**:
```razor
<MudButton Variant="Variant.Filled" 
           Color="Color.Primary" 
           StartIcon="@Icons.Material.Outlined.Add"
           OnClick="@OnAddClicked"
           Disabled="@IsReadOnly">
    Add Item
</MudButton>
```

**Radzen Equivalent**:
```razor
<RadzenButton Text="Add Item"
              Icon="add"
              ButtonStyle="ButtonStyle.Primary"
              Click="@OnAddClicked"
              Disabled="@IsReadOnly" />
```

**Migration Complexity**: Low
- Variant + Color → ButtonStyle
- StartIcon → Icon
- OnClick → Click
- EndIcon → separate icon after text

**ButtonStyle Mapping**:
| MudBlazor | Radzen ButtonStyle |
|-----------|-------------------|
| Variant.Filled + Color.Primary | ButtonStyle.Primary |
| Variant.Filled + Color.Secondary | ButtonStyle.Secondary |
| Variant.Outlined + Color.Primary | ButtonStyle.OutlinePrimary |
| Variant.Text + Color.Primary | ButtonStyle.Light |
| Variant.Filled + Color.Error | ButtonStyle.Danger |
| Variant.Filled + Color.Success | ButtonStyle.Success |
| Variant.Filled + Color.Warning | ButtonStyle.Warning |
| Variant.Filled + Color.Info | ButtonStyle.Info |

---

### MudIconButton → RadzenButton (Icon Only)
**Usage**: 32 occurrences

**MudBlazor**:
```razor
<MudIconButton Icon="@Icons.Material.Outlined.Close" 
               Size="Size.Medium" 
               OnClick="@OnClosedClicked"
               Color="Color.Inherit" />
```

**Radzen Equivalent**:
```razor
<RadzenButton Icon="close"
              ButtonStyle="ButtonStyle.Light"
              Click="@OnClosedClicked"
              Size="ButtonSize.Medium"
              Variant="Variant.Text" />
```

**Migration Complexity**: Low
- Same as MudButton but with Icon only
- Size → ButtonSize enum
- Color.Inherit → ButtonStyle.Light or transparent

---

### MudToggleIconButton → Custom Implementation
**Usage**: 7 occurrences

**MudBlazor**:
```razor
<MudToggleIconButton Icon="@Icons.Material.Outlined.LightMode" 
                     ToggledIcon="@Icons.Material.Outlined.DarkMode"
                     @bind-Toggled="_isDarkMode"
                     Color="Color.Inherit" />
```

**Radzen Equivalent** (Custom):
```razor
<RadzenButton Icon="@(_isDarkMode ? "dark_mode" : "light_mode")"
              ButtonStyle="ButtonStyle.Light"
              Click="@(() => _isDarkMode = !_isDarkMode)"
              Variant="Variant.Text" />
```

**Migration Complexity**: Medium
- No direct toggle button in Radzen
- Need manual toggle state management
- Icon changes based on state

**Alternative**: Create custom ToggleIconButton component wrapping RadzenButton

---

### MudForm → RadzenTemplateForm
**Usage**: 7 occurrences

**MudBlazor**:
```razor
<MudForm @ref="_form" Model="@_model" Validation="@_validator">
    <MudTextField @bind-Value="_model.Name" For="@(() => _model.Name)" />
    <MudButton OnClick="Submit">Submit</MudButton>
</MudForm>
```

**Radzen Equivalent**:
```razor
<RadzenTemplateForm Data="@_model" Submit="@OnSubmit">
    <RadzenTextBox @bind-Value="_model.Name" Name="name" />
    <RadzenRequiredValidator Component="name" Text="Required" />
    <RadzenButton ButtonType="ButtonType.Submit" Text="Submit" />
</RadzenTemplateForm>
```

**Migration Complexity**: Medium
- Different validation approach
- Radzen uses validator components
- FluentValidation integration may need custom work
- Submit handling different

**Notes**:
- MudBlazor uses FluentValidation natively
- Radzen has built-in validators (Required, Email, Compare, etc.)
- May need custom validator for FluentValidation

---

### MudFileUpload → RadzenFileInput
**Usage**: 1 occurrence

**MudBlazor**:
```razor
<MudFileUpload T="IReadOnlyList<IBrowserFile>" 
               FilesChanged="@OnFilesSelected" 
               MaximumFileCount="Int32.MaxValue" />
```

**Radzen Equivalent**:
```razor
<RadzenFileInput Multiple="true" 
                 Change="@OnFilesSelected" 
                 Accept=".json,.zip" />
```

**Migration Complexity**: Low
- T parameter not needed in Radzen
- FilesChanged → Change
- MaximumFileCount → no direct equivalent

---

## Table Components

### MudTable → RadzenDataGrid
**Usage**: 8 occurrences (CRITICAL - core workflow component)

**MudBlazor**:
```razor
<MudTable @ref="_table"
          T="WorkflowDefinitionRow"
          ServerData="ServerReload"
          Dense="true"
          Hover="true"
          Elevation="0"
          OnRowClick="@OnRowClick"
          MultiSelection="true"
          @bind-SelectedItems="_selectedRows">
    <ToolBarContent>
        <MudTextField Placeholder="Search" @bind-Value="_searchTerm" />
    </ToolBarContent>
    <HeaderContent>
        <MudTh><MudTableSortLabel SortLabel="Name">Name</MudTableSortLabel></MudTh>
        <MudTh>Actions</MudTh>
    </HeaderContent>
    <RowTemplate>
        <MudTd DataLabel="Name">@context.Name</MudTd>
        <MudTd><MudIconButton Icon="@Icons.Material.Outlined.Edit" /></MudTd>
    </RowTemplate>
    <PagerContent>
        <MudTablePager />
    </PagerContent>
</MudTable>
```

**Radzen Equivalent**:
```razor
<RadzenDataGrid @ref="_grid"
                Data="@_data"
                TItem="WorkflowDefinitionRow"
                LoadData="@LoadData"
                AllowSorting="true"
                AllowFiltering="true"
                AllowPaging="true"
                PageSize="20"
                PagerHorizontalAlign="HorizontalAlign.Center"
                RowSelect="@OnRowSelect"
                SelectionMode="DataGridSelectionMode.Multiple"
                @bind-Value="_selectedRows">
    <Columns>
        <RadzenDataGridColumn TItem="WorkflowDefinitionRow" Property="Name" Title="Name" />
        <RadzenDataGridColumn TItem="WorkflowDefinitionRow" Title="Actions" Sortable="false">
            <Template Context="row">
                <RadzenButton Icon="edit" ButtonStyle="ButtonStyle.Light" />
            </Template>
        </RadzenDataGridColumn>
    </Columns>
</RadzenDataGrid>
```

**Migration Complexity**: HIGH
- Very different structure: HeaderContent/RowTemplate → Columns
- ServerData → LoadData (different signature)
- Toolbar not built-in (need separate component)
- Selection binding different
- Pager built-in (no PagerContent)

**Major Differences**:

| Feature | MudTable | RadzenDataGrid |
|---------|----------|----------------|
| Structure | Template-based | Column-based |
| Server data | ServerData delegate | LoadData event |
| Toolbar | ToolBarContent | Separate component |
| Sorting | MudTableSortLabel | Column Sortable property |
| Filtering | Manual | Built-in AllowFiltering |
| Selection | @bind-SelectedItems | @bind-Value |
| Pagination | MudTablePager | Built-in pager |
| Row click | OnRowClick | RowSelect |
| Custom cells | MudTd template | Column Template |

**Migration Strategy**:
1. Convert HeaderContent/RowTemplate to Columns definition
2. Refactor ServerData method to LoadData event
3. Move toolbar to separate component above grid
4. Update selection binding
5. Test sorting, filtering, pagination
6. Verify performance with large datasets

**Example ServerData → LoadData**:

MudBlazor:
```csharp
private async Task<TableData<WorkflowDefinitionRow>> ServerReload(TableState state)
{
    var query = new ListWorkflowDefinitionsRequest
    {
        Page = state.Page,
        PageSize = state.PageSize,
        SearchTerm = _searchTerm,
        OrderBy = state.SortLabel,
        OrderDirection = state.SortDirection
    };
    
    var result = await _api.ListAsync(query);
    return new TableData<WorkflowDefinitionRow>
    {
        Items = result.Items,
        TotalItems = result.TotalCount
    };
}
```

Radzen:
```csharp
private async Task LoadData(LoadDataArgs args)
{
    var query = new ListWorkflowDefinitionsRequest
    {
        Page = args.Skip / args.Top,
        PageSize = args.Top,
        SearchTerm = _searchTerm,
        OrderBy = args.OrderBy,
        OrderDirection = args.Ascending ? "Asc" : "Desc"
    };
    
    var result = await _api.ListAsync(query);
    _data = result.Items;
    _count = result.TotalCount;
    await InvokeAsync(StateHasChanged);
}
```

---

### MudSimpleTable → Native HTML Table
**Usage**: 2 occurrences (DataPanel.razor)

**MudBlazor**:
```razor
<MudSimpleTable Outlined="true" Striped="false" Dense="true" Elevation="0">
    <thead>
        <tr><th>Key</th><th>Value</th></tr>
    </thead>
    <tbody>
        <tr><td>@item.Key</td><td>@item.Value</td></tr>
    </tbody>
</MudSimpleTable>
```

**Radzen Equivalent** (Native HTML):
```razor
<table class="rz-datatable">
    <thead>
        <tr><th>Key</th><th>Value</th></tr>
    </thead>
    <tbody>
        <tr><td>@item.Key</td><td>@item.Value</td></tr>
    </tbody>
</table>
```

**Migration Complexity**: Low
- Simple tables can use native HTML
- Apply Radzen table CSS classes
- No need for full DataGrid

---

## Navigation Components

### MudTabs → RadzenTabs
**Usage**: 9 containers, 33 panels

**MudBlazor**:
```razor
<MudTabs Elevation="0" Position="Position.Top" PanelClass="pa-0" Border="true">
    <MudTabPanel Text="@Localizer["Properties"]" Icon="@Icons.Material.Outlined.Settings">
        <!-- Content -->
    </MudTabPanel>
    <MudTabPanel Text="@Localizer["Variables"]">
        <!-- Content -->
    </MudTabPanel>
</MudTabs>
```

**Radzen Equivalent**:
```razor
<RadzenTabs RenderMode="TabRenderMode.Client">
    <Tabs>
        <RadzenTabsItem Text="@Localizer["Properties"]" Icon="settings">
            <!-- Content -->
        </RadzenTabsItem>
        <RadzenTabsItem Text="@Localizer["Variables"]">
            <!-- Content -->
        </RadzenTabsItem>
    </Tabs>
</RadzenTabs>
```

**Migration Complexity**: Medium
- Similar structure
- Position not available (always top)
- Icon support available
- PanelClass → custom CSS
- Border → custom CSS

**Notes**:
- Radzen tabs less customizable
- May need CSS for exact styling
- RenderMode affects performance

---

### MudNavLink → RadzenLink (with routing)
**Usage**: 1 occurrence (NavMenuItem.razor)

**MudBlazor**:
```razor
<MudNavLink Href="@MenuItem.Href" Match="@MenuItem.Match" Icon="@MenuItem.Icon">
    @MenuItem.Text
</MudNavLink>
```

**Radzen Equivalent**:
```razor
<RadzenLink Path="@MenuItem.Href" Icon="@MenuItem.Icon" Text="@MenuItem.Text" />
```

**Migration Complexity**: Low
- Href → Path
- Match prop not available (always exact match)
- Similar functionality

---

### MudNavGroup → RadzenPanelMenuItem
**Usage**: 1 occurrence (NavMenuItem.razor)

**MudBlazor**:
```razor
<MudNavGroup Icon="@MenuItem.Icon" Title="@MenuItem.Text">
    @foreach (var child in MenuItem.Children)
    {
        <MudNavLink Href="@child.Href">@child.Text</MudNavLink>
    }
</MudNavGroup>
```

**Radzen Equivalent**:
```razor
<RadzenPanelMenuItem Text="@MenuItem.Text" Icon="@MenuItem.Icon">
    @foreach (var child in MenuItem.Children)
    {
        <RadzenPanelMenuItem Text="@child.Text" Path="@child.Href" />
    }
</RadzenPanelMenuItem>
```

**Migration Complexity**: Medium
- Used within RadzenPanelMenu
- Similar nested structure
- Expandable by default

---

### MudMenu → RadzenDropDown or RadzenContextMenu
**Usage**: 19 occurrences

**MudBlazor**:
```razor
<MudMenu Label="Actions" Icon="@Icons.Material.Filled.KeyboardArrowDown" Color="Color.Primary">
    <MudMenuItem Icon="@Icons.Material.Outlined.Edit" OnClick="@OnEdit">Edit</MudMenuItem>
    <MudMenuItem Icon="@Icons.Material.Outlined.Delete" OnClick="@OnDelete">Delete</MudMenuItem>
    <MudDivider />
    <MudMenuItem OnClick="@OnExport">Export</MudMenuItem>
</MudMenu>
```

**Radzen Equivalent** (DropDown):
```razor
<RadzenSplitButton Text="Actions" Icon="keyboard_arrow_down" ButtonStyle="ButtonStyle.Primary">
    <RadzenSplitButtonItem Text="Edit" Icon="edit" Value="edit" />
    <RadzenSplitButtonItem Text="Delete" Icon="delete" Value="delete" />
    <RadzenSplitButtonItem Text="Export" Value="export" />
</RadzenSplitButton>
```

**Radzen Equivalent** (ContextMenu):
```razor
<RadzenContextMenu>
    <RadzenMenuItem Text="Edit" Icon="edit" Value="edit" />
    <RadzenMenuItem Text="Delete" Icon="delete" Value="delete" />
    <RadzenMenuItem Text="Export" Value="export" />
</RadzenContextMenu>
```

**Migration Complexity**: Medium
- Two different Radzen components depending on use case
- SplitButton for toolbar menus
- ContextMenu for right-click menus
- Click handling different (Value parameter)
- Divider not available in SplitButton

**Notes**:
- Analyze each MudMenu usage context
- Choose appropriate Radzen component
- May need custom styling

---

## Feedback Components

### MudAlert → RadzenAlert
**Usage**: 19 occurrences

**MudBlazor**:
```razor
<MudAlert Severity="Severity.Warning" Variant="Variant.Filled" Icon="@Icons.Material.Filled.Warning">
    You are running in read-only mode.
</MudAlert>
```

**Radzen Equivalent**:
```razor
<RadzenAlert AlertStyle="AlertStyle.Warning" Variant="Variant.Filled" Icon="warning">
    You are running in read-only mode.
</RadzenAlert>
```

**Migration Complexity**: Low
- Severity → AlertStyle
- Very similar API
- Icon support available

**Severity Mapping**:
| MudBlazor Severity | Radzen AlertStyle |
|-------------------|------------------|
| Normal | Base |
| Info | Info |
| Success | Success |
| Warning | Warning |
| Error | Danger |

---

### MudTooltip → RadzenTooltip
**Usage**: 30 occurrences

**MudBlazor**:
```razor
<MudTooltip Text="@Localizer["Delete"]" Delay="500">
    <MudIconButton Icon="@Icons.Material.Outlined.Delete" OnClick="@OnDelete" />
</MudTooltip>
```

**Radzen Equivalent**:
```razor
<RadzenButton Icon="delete" Click="@OnDelete" 
              MouseEnter="@(args => tooltipService.Open(args, Localizer["Delete"], new TooltipOptions { Delay = 500 }))" />
```

**Alternative (Attribute)**:
```razor
<RadzenButton Icon="delete" Click="@OnDelete" title="@Localizer["Delete"]" />
```

**Migration Complexity**: Low-Medium
- Radzen uses TooltipService instead of component wrapper
- Or use native HTML title attribute
- Delay via TooltipOptions

**Notes**:
- Inject ITooltipService
- Consider native title for simplicity
- TooltipService for advanced scenarios

---

### MudDialog → DialogService + Custom Component
**Usage**: 11 dialog components

**MudBlazor**:
```razor
<!-- Dialog Component -->
<MudDialog>
    <TitleContent>
        <MudText Typo="Typo.h6">Create Workflow</MudText>
    </TitleContent>
    <DialogContent>
        <MudTextField @bind-Value="_model.Name" Label="Name" />
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Cancel">Cancel</MudButton>
        <MudButton Color="Color.Primary" OnClick="Submit">Create</MudButton>
    </DialogActions>
</MudDialog>

<!-- Usage -->
await DialogService.ShowAsync<CreateWorkflowDialog>("New Workflow", parameters, options);
```

**Radzen Equivalent**:
```razor
<!-- Dialog Component (no wrapper needed) -->
<RadzenTemplateForm Data="@_model" Submit="@Submit">
    <RadzenTextBox @bind-Value="_model.Name" Placeholder="Name" />
</RadzenTemplateForm>

<!-- Usage -->
await DialogService.OpenAsync<CreateWorkflowDialog>("New Workflow", 
    new Dictionary<string, object> { { "Model", _model } },
    new DialogOptions { Width = "500px" });
```

**Migration Complexity**: HIGH
- MudDialog structure (TitleContent/DialogContent/DialogActions) → flat structure
- DialogService API different
- Parameters passed differently
- Result handling different

**Dialog Service Comparison**:

| Feature | MudBlazor | Radzen |
|---------|-----------|--------|
| Show dialog | ShowAsync\<T\> | OpenAsync\<T\> |
| Message box | ShowMessageBox | Confirm |
| Parameters | DialogParameters | Dictionary\<string, object\> |
| Options | DialogOptions | DialogOptions (different) |
| Result | DialogResult | Dynamic (dialog return value) |
| Close from component | MudDialog.Close | DialogService.Close |

**Migration Steps**:
1. Remove MudDialog wrapper from dialog components
2. Update DialogService calls (ShowAsync → OpenAsync)
3. Refactor parameter passing
4. Update result handling
5. Add custom title/action buttons
6. Test all dialogs

---

### MudSnackbar (via ISnackbar service) → NotificationService
**Usage**: 64+ references

**MudBlazor**:
```csharp
// Service
_snackbar.Add("Workflow saved successfully", Severity.Success);

// Configuration
services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopCenter;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 3000;
});
```

**Radzen Equivalent**:
```csharp
// Service
_notificationService.Notify(new NotificationMessage
{
    Severity = NotificationSeverity.Success,
    Summary = "Success",
    Detail = "Workflow saved successfully",
    Duration = 3000
});

// Configuration (in Program.cs)
services.AddRadzenComponents();
```

**Migration Complexity**: Medium
- ISnackbar → INotificationService
- Add method → Notify method with NotificationMessage
- Severity enum different
- Configuration approach different

**IUserMessageService Migration**:
```csharp
// Before (MudBlazor)
public class DefaultUserMessageService : IUserMessageService
{
    private readonly ISnackbar _snackbar;
    
    public void ShowSnackbarTextMessage(string message, Severity severity)
    {
        _snackbar.Add(message, severity);
    }
}

// After (Radzen)
public class DefaultUserMessageService : IUserMessageService
{
    private readonly NotificationService _notificationService;
    
    public void ShowSnackbarTextMessage(string message, Severity severity)
    {
        _notificationService.Notify(new NotificationMessage
        {
            Severity = MapSeverity(severity),
            Detail = message,
            Duration = 3000
        });
    }
    
    private NotificationSeverity MapSeverity(Severity severity) => severity switch
    {
        Severity.Normal => NotificationSeverity.Info,
        Severity.Info => NotificationSeverity.Info,
        Severity.Success => NotificationSeverity.Success,
        Severity.Warning => NotificationSeverity.Warning,
        Severity.Error => NotificationSeverity.Error,
        _ => NotificationSeverity.Info
    };
}
```

---

## Service Layer

### IThemeService (MudTheme) → Custom Abstraction
**Usage**: Core service, 64+ references

**Current (MudBlazor-dependent)**:
```csharp
public interface IThemeService
{
    MudTheme CurrentTheme { get; set; }
    Palette CurrentPalette { get; }
    bool IsDarkMode { get; set; }
}
```

**Proposed (Framework-agnostic)**:
```csharp
public interface IThemeService
{
    ITheme CurrentTheme { get; set; }
    IPalette CurrentPalette { get; }
    bool IsDarkMode { get; set; }
}

public interface ITheme
{
    IPalette PaletteLight { get; }
    IPalette PaletteDark { get; }
    ILayoutProperties LayoutProperties { get; }
}

public interface IPalette
{
    string Primary { get; }
    string Secondary { get; }
    string Background { get; }
    string Surface { get; }
    string AppbarBackground { get; }
    string DrawerBackground { get; }
    // ... other colors
}
```

**Radzen Implementation**:
```csharp
public class RadzenThemeAdapter : ITheme
{
    private readonly Theme _radzenTheme;
    
    public IPalette PaletteLight => new RadzenPaletteAdapter(_radzenTheme, false);
    public IPalette PaletteDark => new RadzenPaletteAdapter(_radzenTheme, true);
    public ILayoutProperties LayoutProperties => new RadzenLayoutPropertiesAdapter(_radzenTheme);
}
```

**Migration Complexity**: HIGH
- Core interface change affects 64+ files
- Need abstraction layer for both frameworks
- Parallel support during migration
- Theming system completely different

**Migration Strategy**:
1. Create framework-agnostic theme interfaces
2. Implement adapters for MudBlazor and Radzen
3. Update IThemeService to use abstractions
4. Gradually migrate consumers
5. Remove MudBlazor adapter after migration

---

### IDialogService → Custom Abstraction
**Usage**: 30+ files

**Proposed Abstraction**:
```csharp
public interface ICustomDialogService
{
    Task<IDialogReference> ShowDialogAsync<TDialog>(string title, IDictionary<string, object>? parameters = null, DialogOptions? options = null) 
        where TDialog : ComponentBase;
    
    Task<bool?> ShowConfirmAsync(string title, string message, string yesText = "Yes", string noText = "No");
}

public interface IDialogReference
{
    Task<object?> Result { get; }
    void Close(object? result = null);
}
```

**MudBlazor Implementation**:
```csharp
public class MudDialogServiceAdapter : ICustomDialogService
{
    private readonly IDialogService _mudDialogService;
    
    public async Task<IDialogReference> ShowDialogAsync<TDialog>(...)
    {
        var dialogRef = await _mudDialogService.ShowAsync<TDialog>(title, parameters, options);
        return new MudDialogReferenceAdapter(dialogRef);
    }
}
```

**Radzen Implementation**:
```csharp
public class RadzenDialogServiceAdapter : ICustomDialogService
{
    private readonly DialogService _radzenDialogService;
    
    public async Task<IDialogReference> ShowDialogAsync<TDialog>(...)
    {
        var result = await _radzenDialogService.OpenAsync<TDialog>(title, parameters, options);
        return new RadzenDialogReferenceAdapter(result);
    }
}
```

**Migration Complexity**: HIGH
- 30+ files using DialogService directly
- Need abstraction for both frameworks
- Parameter passing different
- Result handling different

---

## Theming & Styling

### Theme Configuration

**MudBlazor Theme**:
```csharp
var theme = new MudTheme
{
    PaletteLight = new PaletteLight
    {
        Primary = "#0ea5e9",
        DrawerBackground = "#f8fafc",
        AppbarBackground = "#0ea5e9",
        AppbarText = "#ffffff",
        Background = "#ffffff",
        Surface = "#f8fafc"
    },
    PaletteDark = new PaletteDark
    {
        Primary = "#0ea5e9",
        AppbarBackground = "#0f172a",
        DrawerBackground = "#0f172a",
        Background = "#0f172a",
        Surface = "#182234"
    },
    LayoutProperties = new LayoutProperties
    {
        DefaultBorderRadius = "4px"
    }
};
```

**Radzen Theme** (CSS Variables):
```css
:root {
    --rz-primary: #0ea5e9;
    --rz-secondary: #6c757d;
    --rz-info: #0dcaf0;
    --rz-success: #198754;
    --rz-warning: #ffc107;
    --rz-danger: #dc3545;
    --rz-light: #f8fafc;
    --rz-dark: #0f172a;
    --rz-body-background: #ffffff;
    --rz-sidebar-background: #f8fafc;
    --rz-header-background: #0ea5e9;
}

[data-theme="dark"] {
    --rz-body-background: #0f172a;
    --rz-sidebar-background: #0f172a;
    --rz-header-background: #0f172a;
    --rz-surface: #182234;
}
```

**Migration Strategy**:
1. Define custom CSS variables matching MudBlazor theme
2. Create theme switcher for light/dark modes
3. Apply CSS variables to Radzen components
4. Test visual consistency

---

### CSS Classes

**Common MudBlazor CSS Classes to Replace**:

| MudBlazor Class | Radzen Equivalent | Custom CSS |
|----------------|------------------|------------|
| `mud-container` | `rz-container` | ✅ |
| `mud-typography` | Native HTML tags | ✅ |
| `mud-primary-text` | `rz-color-primary` | ✅ |
| `mud-table-cell` | `rz-datatable-data` | N/A |
| `mud-table-toolbar` | Custom div | ✅ |
| `pa-0` (padding 0) | `p-0` | ✅ |
| `ma-6` (margin 6) | `m-6` | ✅ |
| `d-flex` | `d-flex` | Native |
| `flex-row` | `flex-row` | Native |
| `align-center` | `align-items-center` | Native |

**Custom CSS Needed**:
```css
/* Loading Splash */
.loading-splash {
    display: flex;
    justify-content: center;
    align-items: center;
    height: 100vh;
}

.loading-splash h5 {
    color: var(--rz-primary);
    margin: 1.5rem 0;
}

/* Utility Classes */
.pa-0 { padding: 0 !important; }
.ma-6 { margin: 1.5rem !important; }
.mt-10 { margin-top: 2.5rem !important; }
```

---

## Summary

### Component Count by Complexity

**Low Complexity** (60 components):
- MudText, MudIcon, MudCheckBox, MudButton, MudAlert, MudTooltip
- MudContainer, MudPaper, MudStack, MudDivider, MudSpacer
- MudTextField, MudDatePicker, MudFileUpload
- Direct replacements with minimal API differences

**Medium Complexity** (40 components):
- MudSelect, MudTabs, MudTabPanel, MudMenu, MudNavLink, MudNavGroup
- MudForm, MudIconButton, MudField, MudButtonGroup
- API differences, needs refactoring

**High Complexity** (20 components):
- MudTable (→ RadzenDataGrid): Major structural differences
- MudDialog (+ DialogService): Different service API
- MudSelectExtended (→ enhanced RadzenDropDown): Lost features
- MudLayout/MudAppBar/MudDrawer: Complete layout rewrite
- MudToggleIconButton: No direct equivalent

### Service Layer Abstraction Priority

**Critical** (Must abstract first):
1. IThemeService (64+ references)
2. IDialogService (30+ references)
3. IUserMessageService (64+ references via ISnackbar)

**Lower Priority**:
4. TooltipService (30 references, can use native)
5. PopoverService (3 references, minimal usage)

---

**Document Version**: 1.0  
**Last Updated**: 2026-01-06  
**Status**: Draft for Review
