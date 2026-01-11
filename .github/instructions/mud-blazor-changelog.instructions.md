---
description: MudBlazor Version-Specific Guidance and Migration Instructions
applyTo: '**/*.razor, **/*.razor.cs'
---

# MudBlazor Version-Specific Instructions

## Overview

This file provides AI assistants with comprehensive information about MudBlazor versions, helping them:
- Use correct API syntax for the project's MudBlazor version
- Suggest appropriate migration paths when upgrading
- Avoid deprecated or removed features
- Leverage new features introduced in recent versions

## Version Detection

**Always check the project's MudBlazor version before making suggestions:**
1. Read the `.csproj` file to find `<PackageReference Include="MudBlazor" Version="..." />`
2. Adjust recommendations based on the version in use
3. Warn about breaking changes when suggesting upgrades

---

## Current Latest Version: v8.15.0 (November 23, 2025)

### Key v8.x Features (If User is on v7 or Lower)

#### Major Breaking Changes from v7 to v8:
- **MudPopoverProvider Required**: Must explicitly add `<MudPopoverProvider />` in App.razor/MainLayout
- **.NET 7 Support Dropped**: Minimum framework is .NET 8.0
- **Async API Standardization**: Many methods now have `Async` suffix and return `Task`
- **Parameter Name Changes**: `DisableElevation` → `DropShadow`, `DisableGutters` → `Gutters`, etc.
- **ParameterState Framework**: Two-way bindings now use `@bind-*` instead of manual `*Changed` events
- **MudGlobal Defaults**: New global configuration system for component defaults

#### New Components in v8:
- **MudStepper**: Step-by-step workflows with vertical/horizontal layouts
- **MudChat**: Chat interface with messages, avatars, and timestamps
- **MudSankeyChart**: Data flow visualization (v8.14+)
- **MudHeatMap**: Heat map chart for data density visualization (v8.0+)

#### Major Feature Additions:
- **HierarchyColumn in MudDataGrid**: Custom child row rendering (v8.15)
- **MudMenu Improvements**: Cascading submenus, touch support, keyboard navigation
- **MudColorPicker Enhancements**: Palette generation algorithms, IParsable support
- **MudTooltip Touch Support**: Better mobile experience
- **MudTimePicker Touch Dragging**: Drag to select time
- **MudFileUpload File Size Validation**: Built-in size limits

---

## Critical API Changes by Version

### v8.0.0 → v8.15.0 (Latest Stable)

#### v8.15.0 (Nov 2025)
- **MudDataGrid**: Custom child row rendering via `ChildRowContent` parameter
- **MudFormComponent**: `ErrorText` now supports two-way binding (`@bind-ErrorText`)
- **MudDialog/MudPicker**: Hide actions if no buttons provided

#### v8.14.0 (Nov 2025)
- **MudGlobal**: Deprecated theming properties (use CSS variables instead)
- **MudStepper**: Provides step context to child content
- **Icons**: Added Slack brand icon

#### v8.13.0 (Oct 2025)
- **MudTable**: `LoadingContentBody` render fragment for custom loading UI
- **MudSnackbar**: Global `HideIcon` option

#### v8.12.0 (Sep 2025)
- **MudDataGrid**: Hierarchy visibility toggled parameter

#### v8.11.0 (Aug 2025)
- **MudStepper**: Skipped state support
- **MudField**: `AdornmentAria` and `ShrinkLabel` parameters

---

### v7.x → v8.0.0 Migration Guide

**CRITICAL: Read [Official Migration Guide](https://github.com/MudBlazor/MudBlazor/issues/8447) before upgrading!**

#### Required Code Changes:

1. **Add MudPopoverProvider**:
```razor
<!-- Before (v7) -->
<MudThemeProvider />
<MudDialogProvider />
<MudSnackbarProvider />

<!-- After (v8) -->
<MudThemeProvider />
<MudPopoverProvider />  <!-- NEW: Required! -->
<MudDialogProvider />
<MudSnackbarProvider />
```

2. **Update .NET Target Framework**:
```xml
<!-- Before -->
<TargetFramework>net7.0</TargetFramework>

<!-- After -->
<TargetFramework>net8.0</TargetFramework>
```

3. **Async Method Naming**:
```csharp
// Before (v7)
autocomplete.ToggleMenu();
dialog.Close();

// After (v8)
await autocomplete.ToggleMenuAsync();
await dialog.CloseAsync();
```

4. **Parameter Renames**:
```razor
<!-- Before (v7) -->
<MudButton DisableElevation="true" DisableRipple="true">

<!-- After (v8) -->
<MudButton DropShadow="false" Ripple="false">
```

5. **Palette Type Changes**:
```csharp
// Before (v7)
public class MyTheme : MudTheme
{
    public Palette Palette { get; set; } = new Palette();
}

// After (v8)
public class MyTheme : MudTheme
{
    public PaletteLight PaletteLight { get; set; } = new PaletteLight();
}
```

---

### v6.x → v7.0.0 Migration

**[Full v7 Migration Guide](https://github.com/MudBlazor/MudBlazor/issues/8447)**

Key changes:
- **.NET 6 Support Dropped**
- **KeyInterceptor → KeyInterceptorService**
- **MudPopoverService Removed** (use MudPopover directly)
- **Typography Changes**: `FontWeight` and `LineHeight` are now strings

---

## Common Upgrade Patterns

### Checking Component Properties Before Use
```csharp
// AI should verify property existence based on version
// Example: MudFormComponent.ErrorText two-way binding
// Only available in v8.15+
if (mudBlazorVersion >= new Version(8, 15))
{
    // Use @bind-ErrorText
    <MudTextField @bind-ErrorText="errorText" />
}
else
{
    // Use ErrorTextChanged event
    <MudTextField ErrorText="@errorText" ErrorTextChanged="OnErrorTextChanged" />
}
```

### Deprecation Warnings
When suggesting code, check for deprecated features:
- `MudGlobal.EnableIllegalRazorParameterDetection` (removed in v8)
- `KeyInterceptor` (replaced by `KeyInterceptorService` in v8)
- Sync methods without `Async` suffix (v8+ requires async)

---

## Version-Specific Component Behavior

### MudDataGrid

#### v8.15+ (Current)
- Custom child row rendering
- `ChildRowContent` parameter for hierarchical data
- Improved sticky columns with sticky headers

#### v8.0-8.14
- ServerData with virtualization
- Multi-level grouping
- Hierarchy column with expand/collapse all

#### v7.x
- Basic grouping only
- No virtualization with ServerData
- Limited hierarchy support

#### v6.x (Legacy)
- Considered experimental
- Many breaking changes in later versions

**AI Recommendation**: For new MudDataGrid code, target v8.x features.

---

### MudMenu

#### v8.8+ (Current)
- Drag and drop tabs
- Cascading submenus (use `<MudMenu>` inside `<MudMenuItem>`)
- Touch support with pointer events
- Keyboard navigation (Arrow keys, Enter, Escape)

#### v8.0-8.7
- Basic menu with activator
- `OnClick` instead of `OnTouch` (unified in v8.8)

#### v7.x
- Separate `OnTouch` and `OnAction` events
- No cascading support

**Example (v8.8+)**:
```razor
<MudMenu>
    <ActivatorContent>
        <MudButton>Menu</MudButton>
    </ActivatorContent>
    <ChildContent>
        <MudMenuItem>Item 1</MudMenuItem>
        <MudMenuItem>
            Submenu
            <MudMenu ActivationEvent="MouseEvent.MouseOver">
                <ChildContent>
                    <MudMenuItem>Subitem 1</MudMenuItem>
                </ChildContent>
            </MudMenu>
        </MudMenuItem>
    </ChildContent>
</MudMenu>
```

---

### MudAutocomplete

#### v8.10+
- `FoundItemsCount` property for search results count
- `InputClass` parameter for styling input element
- Fixed race conditions in search

#### v8.0-8.9
- `SearchFunc` with `CancellationToken`
- `CoerceValue` with `Strict` mode fixes

#### v7.x
- `SearchFuncWithCancel` (deprecated, use `SearchFunc` with `CancellationToken`)

**Migration**:
```csharp
// Before (v7)
private async Task<IEnumerable<string>> SearchFuncWithCancel(string value, CancellationToken token)
{
    // ...
}

// After (v8)
private async Task<IEnumerable<string>> SearchFunc(string value, CancellationToken token)
{
    // Same implementation
}

<MudAutocomplete SearchFunc="SearchFunc" />
```

---

### MudDialog

#### v8.0+
- `DefaultFocus` parameter (FirstElement, LastElement, None)
- `TitleContent` RenderFragment for custom headers
- `OnKeyDown` / `OnKeyUp` events
- Async `ShowAsync` / `CloseAsync` methods

#### v7.x
- Sync `Show` / `Close` methods (now obsolete)
- Limited focus management

**Example (v8)**:
```razor
<MudDialog>
    <TitleContent>
        <MudText Typo="Typo.h6">Custom Title</MudText>
    </TitleContent>
    <DialogContent>
        <!-- Content -->
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="CloseAsync">Close</MudButton>
    </DialogActions>
</MudDialog>
```

---

### MudForm

#### v8.3+
- `Spacing` parameter for field spacing
- `ReadOnly` and `Disabled` at form level (v7.2+)

#### v7.x-8.2
- Manual field spacing with CSS

**Example**:
```razor
<!-- v8.3+ -->
<MudForm Spacing="4">
    <MudTextField Label="Name" />
    <MudTextField Label="Email" />
</MudForm>
```

---

## Performance Optimizations by Version

### v8.x
- Reduced unnecessary re-renders in inputs
- Async state management with `ParameterState`
- Throttle/debounce for scroll and resize events

### v7.x
- Basic `ParameterState` framework
- Some components still had sync-over-async issues

### v6.x
- Many performance bottlenecks
- Frequent re-renders

**AI Tip**: When working with v8.x, leverage `ParameterState` for two-way bindings to avoid manual `*Changed` event handling.

---

## Accessibility Improvements Timeline

### v8.x (Focus for 2024-2025)
- ARIA labels for pickers
- Keyboard navigation standardization
- Focus trap improvements
- Screen reader support in MudDataGrid

### v7.x
- Basic ARIA support
- `aria-label` parameters added to many components

### v6.x
- Minimal accessibility features

**AI Recommendation**: For new accessibility-critical code, target v8.x for best support.

---

## Common Pitfalls When Upgrading

### 1. Forgetting MudPopoverProvider (v7 → v8)
**Error**: `System.InvalidOperationException: Cannot provide a value for property 'PopoverService'...`

**Fix**: Add `<MudPopoverProvider />` to App.razor/MainLayout.

### 2. Async Method Calls (v7 → v8)
**Error**: CS1061: ... does not contain a definition for 'ToggleMenu'...

**Fix**: Add `Async` suffix and `await`:
```csharp
// Before
autocomplete.ToggleMenu();

// After
await autocomplete.ToggleMenuAsync();
```

### 3. Parameter Name Changes (v7 → v8)
**Warning**: Parameter 'DisableElevation' not recognized.

**Fix**: Use renamed parameters:
- `DisableElevation` → `DropShadow` (inverted logic!)
- `DisableGutters` → `Gutters` (inverted)
- `DisableRipple` → `Ripple` (inverted)
- `DisableUnderLine` → `Underline` (inverted)

### 4. Palette Type Mismatch (v7 → v8)
**Error**: Cannot convert type 'Palette' to 'PaletteLight'

**Fix**: Update theme class properties from `Palette` to `PaletteLight`.

---

## AI Assistant Decision Tree

### When Suggesting Code:

1. **Detect MudBlazor Version**
   - Read `.csproj` PackageReference
   - If version < 8.0: Check v7 docs
   - If version >= 8.0: Use v8 syntax

2. **Check Feature Availability**
   - New component? Verify version introduced
   - Parameter exists? Check changelog
   - Method async? v8+ requires `Async` suffix

3. **Warn About Breaking Changes**
   - Upgrading from v7 to v8? List required changes
   - Deprecation warnings for v6 code

4. **Suggest Best Practices**
   - v8+: Use `@bind-*` for two-way bindings
   - v8+: Always `await` async methods
   - v8+: Use `MudGlobal` for component defaults

---

## Quick Reference: Common Components by Version

| Component | v6 | v7 | v8 | Notes |
|-----------|----|----|----|----|
| MudDataGrid | ⚠️ Experimental | ✅ Stable | ✅ Enhanced | v8: Hierarchy, virtualization |
| MudStepper | ❌ | ❌ | ✅ NEW | Introduced in v8.0 |
| MudMenu | ✅ | ✅ | ✅ Enhanced | v8: Cascading, touch |
| MudAutocomplete | ✅ | ✅ | ✅ Improved | v8: Race conditions fixed |
| MudDialog | ✅ | ✅ | ✅ Enhanced | v8: Async, focus management |
| MudPopoverProvider | ❌ | ❌ | ✅ **REQUIRED** | v8: Must add to App.razor |

---

## Changelog Structure Reference

### Breaking Changes
Changes that require code modifications when upgrading. **Always check these first!**

### New Features
New components, parameters, or methods introduced.

### Bug Fixes
Issues resolved in the version. Useful for understanding behavior changes.

---

## Example: Version-Aware Code Generation

```csharp
// AI generates this for v8.15+
<MudTextField @bind-Value="model.Name"
              @bind-ErrorText="errorText"
              Label="Name"
              Required="true" />

// AI generates this for v7.x-8.14
<MudTextField @bind-Value="model.Name"
              ErrorText="@errorText"
              ErrorTextChanged="OnErrorTextChanged"
              Label="Name"
              Required="true" />

@code {
    private string errorText = "";
    private void OnErrorTextChanged(string value) => errorText = value;
}
```

---

## Full Changelog Archive

Below is the complete MudBlazor changelog for reference. AI assistants should search this content when:
- User asks about specific version behavior
- Checking if a feature exists in a version
- Investigating breaking changes between versions

---

## Complete Version History

### Version v8.15.0 (Kasım 23, 2025)

What's Changed
New Features
MudTabs: Add onmousedown and oncontextmenu events by @DrastamatSargsyan in #11966
SankeyChart: Add multiple convenience features by @91378246 in #12064
MudDialog, MudPicker: Hide empty actions render fragment by @danielchalmers in #12093
MudDataGrid: Custom Child Row Rendering by @csteeg in #11594
MudFormComponent: Make ErrorText property two-way bindable by @soridev in #12119
Bug Fixes
MudDrawer: Fix Persistent Anchor Top & Bottom (#11392) by @redgp0g in #11695
MudTabs: Refactor state management and fix resize/scroll bugs by @versile2 in #12011
MudDataGrid, MudTable: Fix Mobile Loading Bar by @versile2 in #12102
MudAutocomplete: Fails to reopen when nested in MudMenu by @Nickztar in #12021
MudOverlay: Fix scrollable background with visible dialog by @timmac-qmc in #12121
PopoverOptions: Fix missing property assignment by @soridev in #12125
MudBaseInput: Fix OnParametersSet for ParameterState by @ScarletKuro in #12136
Other Changes
Fix misc typos by @khanhkhanhlele in #12061, #12062, #12073, #12068, #12069, #12070, #12071, #12072
New Contributors
@khanhkhanhlele made their first contribution in #12061
@redgp0g made their first contribution in #11695
@csteeg made their first contribution in #11594
@soridev made their first contribution in #12119
Full Changelog: v8.14.0...v8.15.0

Version v8.14.0
Released on Kasım 05, 2025

What's Changed
Breaking Changes
MudGlobal: Deprecate theming properties by @danielchalmers in #11415
New Features
MudColorPicker: Add opt-out tooltips for icon buttons by @danielchalmers in #11953
MudMenu: Enhance keyboard accessibility and focus management by @nccadman19 in #11762
MudTable: Add AriaLabel parameter for accessibility by @dev-KingMaster in #11995
Pickers: Add InputId parameter by @rasmus-carlsson in #12003
Icons: Add Slack brand icon to Icons.Custom.Brands by @codomposer in #12057
MudStepper: Provide step context to child content by @dev-KingMaster in #12054
Bug Fixes
MudDataGrid: Fix generated EditTemplates not inheriting their column's culture by @benditorok in #11931
CSS: Improve browser compatibility by replacing `from ... r g b` syntax by @danielchalmers in #11939
MudDialog: Fix gaps in body style by @danielchalmers in #11506
MudIconButton: Fix filled buttons losing background color on click by @igotinfected in #11944
MudDialog: Force titles to wrap & prevent close button overlap by @RabindranathChanda in #11921
MudPopover: Fix JS Typo by @versile2 in #11968
MudFormComponent: Clear converter errors on ResetValidation by @ArfitAP in #11971
MudScrollListener: Fix document scroll by @versile2 in #11978
MudTooltip: Fix FullWidth children not stretching by @FreskOoAs in #11948
MudAutocomplete: ProgressBar AdornmentIcon Hiding by @versile2 in #11980
MudTabs: Fix TabWrapperContent and Tooltip display with Drag and Drop by @versile2 in #12009
MudSelect: Display selected value when null by @rasmus-carlsson in #12001
ParameterState: Add Snapshot to avoid race condition by @ScarletKuro in #12023
MudNavGroup: Fix Background Color After Click by @mckaragoz in #11989
MudDatePicker: Fix FixedDay, FixMonth, FixYear for non-Gregorian calendar by @oxayotl in #11992
MudAutocomplete: Fix input not focused on adornment click by @digitaldirk in #11970
MudAutocomplete: OpenChanged called twice after selection by @Yomodo in #11985
MudCard: Hide gap when MudCardActions is empty by @danielchalmers in #12033
MudExpansionPanel: Fix Gutters property not affecting header padding by @Copilot in #12036
Other Changes
SankeyChart: Add chart for displaying dataflow by @91378246 in #11919
New Contributors
@benditorok made their first contribution in #11931
@RabindranathChanda made their first contribution in #11921
@ArfitAP made their first contribution in #11971
@FreskOoAs made their first contribution in #11948
@dev-KingMaster made their first contribution in #11995
@codomposer made their first contribution in #12057
Full Changelog: v8.13.0...v8.14.0

Version v8.13.0
Released on Ekim 01, 2025

What's Changed
New Features
MudTable: Add LoadingContentBody render fragment by @Enderlook in #11718
MudTable: Scroll to row & focus cell by @Anu6is in #11569
MudSnackbar: Add global HideIcon option by @franco-diaz-licham in #11900
MudDataGrid: Add parameters for filter icons by @CyrilArtFX in #11864
Bug Fixes
MudIconButton: Fix icon button with default color not having focus indicator by @igotinfected in #11858
MudCheckbox: Fix Tri-State required validation by @Anu6is in #11886
MudCarousel: Fix UI update of bullets on item add by @Blocko07 in #11873
MudDataGrid: Hierarchy ExpandAll/CollapseAll when one item is present by @versile2 in #11904
MudTabs: Guard for ScrollPrev by @versile2 in #11924
New Contributors
@Blocko07 made their first contribution in #11873
@franco-diaz-licham made their first contribution in #11900
@CyrilArtFX made their first contribution in #11864
Full Changelog: v8.12.0...v8.13.0

Version v8.12.0
Released on Eylül 02, 2025

What's Changed
New Features
MudDataGrid: Add hierarchy visibility toggled parameter by @GeorgeKarlinzer in #11706
MudChart: Support custom ToString func for Y axis by @IchBinAb4los in #11674
Bug Fixes
MudPopover: Maintain Scroll Position by @versile2 in #11765
MudTextField: Fix padding for outlined multiline fields by @rasmus-carlsson in #11776
MudDataGrid: Fix nested group expansion for multilevel grouping (#11725) by @M4ntax in #11729
MudDialog: Fix when a dialog is canceled, it can be reopened by @vernou in #11804
MudDataGrid : Refresh on collection changed by @vernou in #11822
MudDataGrid: SCSS, Clean Up Sticky Columns by @versile2 in #11800
MudAutocomplete: Fix `Strict=false` when using a `Converter` with `CoerceValue` by @Mr-Technician in #11826
New Contributors
@rasmus-carlsson made their first contribution in #11776
@M4ntax made their first contribution in #11729
@IchBinAb4los made their first contribution in #11674
@Copilot made their first contribution in #11810
Full Changelog: v8.11.0...v8.12.0

Version v8.11.0
Released on Ağustos 04, 2025

What's Changed
New Features
MudField: AdornmentAria and ShrinkLabel by @versile2 in #11669
MudStepper: Add Skipped State (#11739) by @Alex-DeLaet in #11742
Bug Fixes
MudTable: Maintains group row state when group list is modified by @vernou in #11681
MudTextField: Fix Focus by ElementReference by @versile2 in #11733
MudMenu: Hide menu list if it has no items by @danielchalmers in #11740
MudDataGrid: Fix HierarchyColumn Expansion with Funcs by @versile2 in #11292
MudTabs: Improve accessibility by @nccadman19 in #11581
MudTable : Fix bug where CurrentPage is reset at the initialization by @vernou in #11745
MudPopover: MudList MaxHeight Adjustments by @versile2 in #11734
Other Changes
Community Extensions: Add MudX by @versile2 in #11692
MudPopover: Fix MudList Scroll Issue by @versile2 in #11694
MudTimeSeriesChart: Use InvariantCulture when rendering opacity (#11696) by @svarnyjunak in #11697
MudHighlighter: Maintain markup formatting when Markup=true by @Anu6is in #11705
MudBaseInput: Fix expired ParameterView in inherited components by @GeorgeKarlinzer in #11715
New Contributors
@svarnyjunak made their first contribution in #11697
@GeorgeKarlinzer made their first contribution in #11715
@Alex-DeLaet made their first contribution in #11742
Full Changelog: v8.10.0...v8.11.0

Version v8.10.0
Released on Temmuz 19, 2025

What's Changed
Breaking Changes
MudStepper: Remove StepStyle property by @danielchalmers in #11589
New Features
DataGrid: Extend DateOnly filtering support for Simple and ColumnFilterMenu modes by @raimana in #11599
Bug Fixes
MudTextField: Fix excessive height with AutoGrow, Outlined, Dense, and Adornment by @mathdx1111 in #11600
MudFocusTrap: Prevent JSException on Disposed Component by @jHabjanMXP in #11586
MudTabs: Update ActiveIndex On Render and Refresh when Drag and Drop Enabled by @versile2 in #11568
MudDataGrid: AllowUnsorted Default Behavior by @versile2 in #11655
MudSimpleTable: Remove redundant bottom border from the last row header by @9PK4U in #11628
Other Changes
MudPopover: Restrict Max Height Adjustments by @versile2 in #11680
New Contributors
@9PK4U made their first contribution in #11628
Full Changelog: v8.9.0...v8.10.0

Version v8.9.0
Released on Temmuz 01, 2025

What's Changed
New Features
MudDataGrid: Add AllowUnsorted property by @christianielsen in #11443
Bug Fixes
MudDialog: Fix Inline Dialog Title Refresh by @Anu6is in #11504
MudHighlighter: Fix Highlighted Markup Render Bug by @Anu6is in #11543
MudMask: Update value on browser autofill by @Anu6is in #11547
MudAutocomplete: Fix Strict mode regression by @digitaldirk in #11533
MudSimpleTable: Fix missing row borders in nested tables (#11559) by @n0spaces in #11566
MudProgressCircular: Calculate view box size based on stroke width by @Anu6is in #11557
MudSelect: Always call `FieldChanged` when `MultiSelection=true` by @Mr-Technician in #11553
Other Changes
MudDialog: Fix CloseOnEscapeKey Not Triggered After Backdrop Click by @Anu6is in #11475
Datagrid: Support Dateonly filter by @raimana in #11535
MudExpansionPanel: Add toggle keyboard hotkeys, Add ability to remove content from DOM by @nccadman19 in #11531
New Contributors
@raimana made their first contribution in #11535
@AmpF5 made their first contribution in #11548
@nccadman19 made their first contribution in #11531
@n0spaces made their first contribution in #11566
@christianielsen made their first contribution in #11443
Full Changelog: v8.8.0...v8.9.0

Version v8.8.0
Released on Haziran 17, 2025

What's Changed
New Features
MudAutocomplete: Scroll to selected item when not strict by @digitaldirk in #11049
MudMenu: Add min width & submenu icon gutters by @danielchalmers in #11435
MudDataGrid Default GroupTemplate At Grid Level by @peterthorpe81 in #11237
Improve accessibility labels and localize picker buttons by @danielchalmers in #11456
MudTabs: Add Drag and Drop functionality by @versile2 in #11267
MudGlobal: Add PopoverDefaults.ModalOverlay to control Modal parameters by @rabidsheep in #11486
MudFileUpload: Add file size validation by @Anu6is in #11476
MudToggleGroup: Refactor borders, Add border opacity support by @danielchalmers in #11496
Bug Fixes
MudMenu: Hide popover overflow by @danielchalmers in #11444
MudMenu: Position At Cursor Flip Logic by @versile2 in #11437
MudDataGrid: Filters and Options Popover changed to Position At Cursor by @versile2 in #11452
MudTextField: Fix partially hidden scrollbar, Focus field on label click by @mathdx1111 in #11402
MudDataGrid: Groupable Logic by @versile2 in #11383
MudTablePager: Fix InfoFormat ignoring custom text (#11466) by @sirdx in #11468
MudMenu: Revert previous refactor by @danielchalmers in #11484
MudAutocomplete: Fix item selection stealing focus back after handler completes by @danielchalmers in #11482
MudTabs: Fix MudTabSlider not showing by @sirdx in #11467
MudDrawer: Fix stall when loading by @timmac-qmc in #11009
MudInput: Dispose iOS Blur With Error Handling by @versile2 in #11436
Other Changes
Build: Remove "needs review" label from new PRs by @danielchalmers in #11431
MudToggleItem: Enforce direct parent hierarchy by @danielchalmers in #11498
New Contributors
@ecrocombe made their first contribution in #11427
@mathdx1111 made their first contribution in #11402
@sirdx made their first contribution in #11468
@oxayotl made their first contribution in #11474
@rabidsheep made their first contribution in #11486
Full Changelog: v8.7.0...v8.8.0

Version v8.7.0
Released on Mayıs 30, 2025

What's Changed
New Features
DataGridExtensions: support GridStateVirtualize and sort comparer by @ahjephson in #11270
MudTabPanel: Implement Sorting by Text, SortKey, or Custom IComparer by @richardhauer in #10803
SwipeArea: Real Time Swipe Support by @mckaragoz in #11307
Pickers: Use `InputDefaults` for inputs in `Picker` components by @igotinfected in #11342
MudTable: Add TableClass parameter #11351 by @nathanhqws in #11352
MudCheckBox: Use disabled style on text by @boukenka in #11356
MudCircularProgress: Add ChildContent to Show Centered Content in the Progress by @mckaragoz in #11271
MudColorPicker: Reduce default throttle by @danielchalmers in #11368
MudPopover: Expand Flip Logic to Include Anchor by @versile2 in #11248
MudTablePager: Add thousands separator for all_items by @RafBorrelli in #11371
Add `d-contents` display class, Fix missing gap between MessageBox action buttons by @danielchalmers in #11391
MudDataGrid: Form Dialog Focus by @versile2 in #11379
MudColorPicker: Improve ARIA labels & doc page wording by @danielchalmers in #11401
Palette: Add BorderOpacity property by @danielchalmers in #11394
MudAutocomplete: Allow changing `LockScroll` value by @igotinfected in #11414
Bug Fixes
Popover: Initial Show When Open="true" by @versile2 in #11277
MudCarousel: Utilize ParameterState by @Anu6is in #11324
MudBooleanInput, MudRating: Improve accessibility by @igotinfected in #11084
PopoverProvider: Multiple Layouts by @versile2 in #11305
MudInput: Fix Disposal of IOS Blur by @versile2 in #11347
MudOverlay: Check JSRuntime by @versile2 in #11282
MudToggleGroup: Fix MudToggleItem selection when asynchronously loaded by @jimitndiaye in #11309
MudToggleGroup: Fix FixedContent Visual Options by @mckaragoz in #11306
MudInput: Fix Proper Blur by @versile2 in #11376
MudAutocomplete: Fix `AutoFocus` not opening item list by @danielchalmers in #11365
DataGrid: GroupingByOrder / Style Fix / Expansion StateHasChanged by @versile2 in #11302
Line chart: Fix index out of bounds error by @Anu6is in #11381
MudMenu: Fix submenu activators being too wide by @danielchalmers in #11367
MudTabs: Fix slider misalignment when component is scaled by @liamAU28910 in #11405
MudMask: Fix `⌘` shortcuts on MacOS & `cut` shortcut in general by @igotinfected in #11366
MudPopover: Fix Event Order by @versile2 in #11389
MudToggleGroup: Selection Tracking by @versile2 in #11395
Docs: Fix mangled `AppbarButtons.razor.cs` merge by @danielchalmers in #11419
MudExpansionPanel: Fix `Gutters` property not taking effect by @danielchalmers in #11408
Other Changes
GitHubActions: Fix TryMudBlazor deploy by @Garderoben in #11257
New Contributors
@ahjephson made their first contribution in #11270
@richardhauer made their first contribution in #10803
@nathanhqws made their first contribution in #11352
@liamAU28910 made their first contribution in #11405
Full Changelog: v8.6.0...v8.7.0

Version v8.6.0
Released on Nisan 29, 2025

What's Changed
New Features
MudOverlay: Add parameter Modal allowing click-through by @Cybrosys in #10893
MudDataGrid: Hierarchy Column Header by @versile2 in #11181
MudDateRangePicker: BlurAsync() MudRangeInput can now blur both inputs at once by @urbanambroz in #11104
MudDataGrid: Multi Level Grouping by @ScarletKuro in #11243
Bug Fixes
MudDateRangePicker: Re-enable selecting end date before start date (fix regression) by @Anu6is in #11126
MudInput - Heap Locked When Disabled by @versile2 in #11135
BarChart: Fix tooltip display value by @Anu6is in #11150
MudTimeSeriesChart: Fix Unhandled Exception by @Anu6is in #11153
MudPicker: Set TextUpdateSuppression based on Readonly state by @Anu6is in #11144
MudAutocomplete: Fix various activation issues by @danielchalmers in #11130
MudDataGrid: Enhance HierarchyColumn for RTL and Expand/Collapse All by @versile2 in #11103
MudCarousel: Fix timer creation after component disposal by @jHabjanMXP in #11192
MudPopover: Javascript refactor by @versile2 in #11165
MudChat: RTL Display / Remove Expiramental Tag by @versile2 in #11219
mudElementReference.js: Fix null exception by @phamtuanit in #11222
MudThemeProvider: Only wrap multi-word font names in quotes in CSS font-family declarations by @Garderoben in #11214
Popover: Z-Index Alignment by @versile2 in #11220
MudOverlay: Prevent unnecessary LockScroll changes by @versile2 in #11174
MudInput: Dispose DotNetObjectReference by @versile2 in #11254
Other Changes
MudMenu: Revert hide delay to match show delay again by @danielchalmers in #11129
New Contributors
@jHabjanMXP made their first contribution in #11192
@MiroslavKabat made their first contribution in #11198
@phamtuanit made their first contribution in #11222
@urbanambroz made their first contribution in #11104
Full Changelog: v8.5.1...v8.6.0

Version v8.5.1
Released on Mart 31, 2025

What's Changed
Bug Fixes
Analyzer: Lower the Roslyn version by @ScarletKuro in #11116
MudMenu Nested Fix by @versile2 in #11117
Full Changelog: v8.5.0...v8.5.1

Version v8.5.0
Released on Mart 30, 2025

What's Changed
Bug Fixes
MudChart: Fix culture and perfomance issues by @radderz in #11041
MudSwitch: Change track color to improve visibility on dark theme by @boukenka in #11077
MudTextField: Fix AutoGrow Adornment padding by @Anu6is in #10932
MudInput: Fix trimming issue by @ScarletKuro in #11029
MudTable: Fix table sort label style by @danielchalmers in #11022
MudDateRangePicker: Fix error with MaxDays and IsDateDisabledFunc by @Anu6is in #11033
MudPopover: Remove shadow if the popover is empty by @danielchalmers in #11040
MudAutocomplete: Fix duplicate search calls when opened on click by @danielchalmers in #11023
MudPopover: Overlay Position Exclusion by @versile2 in #11038
ObserverManager: Optimize to reduce dictionary lookups by @ScarletKuro in #11067
DataGrid: Mutlisort key fix on mac by @tomasgreen in #11070
MudDialog : Duplicate dialog fix by @timmac-qmc in #11048
MudSelect: Add FitContent Parameter by @Anu6is in #10894
MudToolTip: Move Duration Delay by @versile2 in #11051
MudInput Dispose JS OnBlur by @versile2 in #11039
MudDatePicker: Fix selecting DateTime.MaxValue (#7285) by @LukasMerz in #7286
MudPopover: Fix positioning delay (partial revert of #10856) by @versile2 in #11107
Analyzers/SourceGenerator: Update Roslyn by @ScarletKuro in #11110
MudMenu: Autocomplete/Select adornment click no longer causes menu to close by @versile2 in #11011
MudTreeViewItem: Fix NullReferenceException by @masonwheeler in #11108
MudDataGrid - Preserve Selection with ParameterState by @versile2 in #11032
New Contributors
@tomasgreen made their first contribution in #11070
@LukasMerz made their first contribution in #7286
@masonwheeler made their first contribution in #11108
Full Changelog: v8.4.0...v8.5.0

Version v8.4.0
Released on Mart 16, 2025

> [!CAUTION] > We don't recommend to use this version. > Since the MudMenu and Charts are broken.
What's Changed
New Features
MudDateRangePicker: Limit Date Range selection based on min/max days constraint by @Anu6is in #10769
MudChart: Timeseries charting improvements by @radderz in #10865
MudTable: Add CellClass parameter by @boukenka in #10884
Bug Fixes
MudProgressLinear: Use MudGlobal.Rounded as default. by @Devqon in #10944
Fix usages of MudGlobal.Rounded by @Devqon in #10957
MudDialog: Fix duplicate dialogs by @timmac-qmc in #10979
MudRadio: Fix incorrect aria checked radio by @ralvarezing in #10965
MudChart/Heatmap: Add Min/Max Overrides by @versile2 in #10985
MudPopover: Adjust Overlay Add Throttle and Debounce by @versile2 in #10856
Prevent Rendering of unused MudPopover and MudOverlay by @versile2 in #10853
DataGrid: SelectedItem(s) ParameterState Refactor by @versile2 in #10829
MudInput: Implement Streamlined Blur event by @versile2 in #10842
MudCollapse: Apply scroll to expanded content overflow by @Anu6is in #10948
MudButtonGroup: Fix some disabled styles not applying to buttons by @danielchalmers in #10999
MudSelect: Fix menu toggled on non-primary mouse clicks by @versile2 in #11002
Popover: Fix Overlay Z-Index issue by @versile2 in #10994
MudStepper: Fix final step completed color by @Lewis-Pitman in #11012
MudTabs: Fix underline display for active tab (#10970) by @charles7668 in #10974
Other Changes
Refactor: Fix code styling, tests by @ScarletKuro in #10993
New Contributors
@Devqon made their first contribution in #10944
@Lewis-Pitman made their first contribution in #11012
Full Changelog: v8.3.0...v8.4.0

Version v8.3.0
Released on Şubat 23, 2025

What's Changed
New Features
MudExpansionPanels: add Panels property to expose child components by @pwasilewski in #10789
MudTable: Show the default cursor for sortable headers when sorting is disabled by @Qwertyluk in #10703
MudStack: Responsive Breakpoint Attributes by @mouse0270 in #10596
MudTabPanel: Add Visible property by @Apflkuacha in #10797
Bug Fixes
MudInput: Fix label text being cut vertically by @DoobieAsDave in #10780
MudNumericField: Ignore default UI Culture by @Anu6is in #10796
MudDropContainer: allow scrolling on disabled dragging by @Apflkuacha in #10781
MudTextField: Fix break style if label text too long by @charles7668 in #10805
MudSelect: Fix ResetAsync by @Anu6is in #10828
MudDataGrid: Fix Loading Indicator scrolls out of view with FixedHeader by @versile2 in #10838
FilterHeaderCell: Display filter icons based on ShowFilterIcon value by @thestahan in #7729
MudAutocomplete: Fix unintended autofocus when adornment clicked by @versile2 in #10837
MudDataGrid: Use key row item instead of row index. by @Jaron-jp in #10845
MudChip: Fix memory allocation by @ScarletKuro in #10913
MudForm: Fire IsTouchChanged when ChildForm triggers by @versile2 in #10811
MudChart: Fix radius error in donut chart (#8780) by @Felixlundmark in #10921
MudDatePicker: Fix Persian year in header by @Anu6is in #10854
Other Changes
CSS: Fix CSS issues reported by sonar by @igotinfected in #10757
Refactor: Fix formatting by @ScarletKuro in #10859
Refactor: Fix code style by @ScarletKuro in #10892
New Contributors
@thestahan made their first contribution in #7729
@Jaron-jp made their first contribution in #10845
@tgothorp made their first contribution in #10883
@Felixlundmark made their first contribution in #10921
Full Changelog: v8.2.0...v8.3.0

Version v8.2.0
Released on Şubat 01, 2025

What's Changed
New Features
MudTabPanel: Add BadgeIcon property by @pwasilewski in #10694
Bug Fixes
MudInput: Fix adornment click passing through to field by @danielchalmers in #10737
MudTooltip: Remove `width:fit-content` by @Anu6is in #10720
MudDropContainer: Re-enable content selection by @Anu6is in #10741
MudDataGrid: Grouping Icon RTL Support by @versile2 in #10743
MudTabs: Fix scroll button appearing when zooming page (#10686) by @sirWest in #10760
MudAutocomplete, MudSelect: Fix Focus and Clear by @versile2 in #10740
MudPopover: Fix Z-Index edge cases by @versile2 in #10735
MudNumericField: Ignore default UI Culture by @Anu6is in #10734
MudProgressLinear: prevent buffer clipping on Medium/Large sizes by @pwasilewski in #10782
MudBaseDatePicker: Don't try to highlight invalid dates by @Anu6is in #10785
MudDateRangePicker: Fix highlight for single day range by @Anu6is in #10783
MudEventManager: Correctly handle TouchList properties in touch-related events by @pwasilewski in #10714
New Contributors
@Cybrosys made their first contribution in #10728
@sirWest made their first contribution in #10760
Full Changelog: v8.1.0...v8.2.0

Version v8.1.0
Released on Ocak 26, 2025

> [!CAUTION] > We don't recommend to use this version.
What's Changed
New Features
MudImage: Add fallback source by @Anu6is in #10599
MudThemeProvider: Add ClassName Style tag by @versile2 in #10667
MudDataGrid: remove detrimental ChildRowContent CSS by @versile2 in #10581
MudDateRangePicker: Allow select end before start by @Apflkuacha in #10680
MudTable: Enable text selection in sortable table header columns by @Qwertyluk in #10615
MudDataGrid: Remove sort when clicking after descending sort by @d4npia in #10623
Bug Fixes
MudDataGrid: Fix Column Options Popover FixedHeader positioning by @versile2 in #10580
MudDataGrid: Sticky toolbars (table menu bar & pagination bar) by @Anu6is in #10386
MudTextField: Fix line height for outlined frame by @Apflkuacha in #10649
MudPagination: Restore list style CSS by @danielchalmers in #10657
MudOverlay: Introduce `mud-skip-overlay-section` by @versile2 in #10666
MudDateRangePicker: Fix Outlined Variant label rendering by @Anu6is in #10670
MudInput: Fix outline label conflict with third-party CSS by @danielchalmers in #10672
Analyzer: Fix HTMLAttributes mode defaults by @peterthorpe81 in #10678
MudDatePicker/MudDateRangePicker: Open static picker on bound date by @Anu6is in #10582
MudTextField : With Required parameter, shows error when only one space is entered by @vernou in #10671
DialogService: Return DialogHelperComponent by @ScarletKuro in #10682
MudInput: Fix legend style incorrectly inherited to child components by @charles7668 in #10683
MudTreeViewItem: make Items two-way for returning server-loaded children by @henon in #10684
MudGlobal: Inherit TooltipDefaults from TransitionDefaults by @danielchalmers in #10679
RegexMask: Update `RegexMask.Email` to allow hyphens (`-`) in the domain by @Anu6is in #10693
MudPicker/MudDateRangePicker: Only highlight selected date by @Anu6is in #10629
MudDropContainer: Fix drag preview "ghost" glitch by @Anu6is in #10652
Select: Don't open while scrolling via touch by @danielchalmers in #10718
MudAutocomplete: Restore `ScrollToListItemAsync` API method by @danielchalmers in #10717
New Contributors
@Apflkuacha made their first contribution in #10649
@phmatray made their first contribution in #10654
@aybti made their first contribution in #10677
@d4npia made their first contribution in #10623
Full Changelog: v8.0.0...v8.1.0

Version v8.0.0
Released on Ocak 18, 2025

> [!NOTE] > If you are migrating from v7.x.x to v8.0.0 please make sure to read the [v8.0.0 Migration Guide](https://github.com/MudBlazor/MudBlazor/issues/9953) !! You can give us your [feedback about v8](https://github.com/MudBlazor/MudBlazor/discussions/10003), it'll be appreciated.
What's Changed
Breaking Changes
Build: Drop support for .NET 7 by @danielchalmers in #9511
Theme: Change Typography members to type BaseTypography by @danielchalmers in #9434
MudTable: Rename MudBlazorFix namespace to MudBlazor by @ScarletKuro in #9952
Rename utilities to fix typos by @danielchalmers in #9582
Typography: Make `FontWeight` and `LineHeight` strings by @danielchalmers in #9592
MudGlobal: Remove `EnableIllegalRazorParameterDetection` by @danielchalmers in #9580
Remove KeyInterceptor, Replaced by KeyInterceptorService by @ScarletKuro in #9956
Remove MudPopoverService by @ScarletKuro in #9957
Key handling followup work by @danielchalmers in #9921
KeyInterceptorService: Make KeyOptions immutable by @ScarletKuro in #9969
DialogService: Remove obsolete OnDialogInstanceAdded by @ScarletKuro in #9980
MudDropContainer: Remove obsolete API by @ScarletKuro in #9981
MudDataGrid: Remove obsolete CancelledEditingItem by @ScarletKuro in #9982
JsApiService: Make implementation internal & add xmldoc. by @ScarletKuro in #9994
JsEvent: Make implementation internal & add xmldoc. by @ScarletKuro in #9996
IJSRuntimeExtensions: Adjust pre-rendering handling for .NET8 by @ScarletKuro in #9997
Localization: Remove obsolete code by @meenzen in #10001
EventUtil: Propagate exceptions. by @ScarletKuro in #9967
TimeSeries: Rename `TimeSeriesDiplayType` to fix typo by @danielchalmers in #9581
MudCharts: Update Namespace, set some classes visibility to internal by @ingkor in #9919
Add `Typography` suffix to all typography classes by @danielchalmers in #9962
MudDialog: Don't override `Class` param and convert BackgroundClass to getter by @henon in #9909
MudFileUpload: fix `AppendMultipleFiles` keeping stale file references (#9586) by @igotinfected in #9600
Typography: Remove `input` in favor of `subtitle1` by @danielchalmers in #10028
Components: Use IAsyncDisposable by @ScarletKuro in #10037
MudSelectItem, MudNavLink: Remove MudBaseSelectItem by @ScarletKuro in #10045
Radio, Check Box, Switch: Unification and improved alignaments by @ralvarezing in #9472
Scroll services: Clean up, make implementations internal, add xmodoc by @ScarletKuro in #10048
EventListener: Clean up, make implementations internal, add xmodoc by @ScarletKuro in #10051
ResizeObserver: Clean up, make implementations internal, add xmodoc by @ScarletKuro in #10055
JsEvent: Use IAsyncDisposable by @ScarletKuro in #10061
MudCollapse: Rework animation by @danielchalmers in #10056
Inputs: Refactor adornment related code by @danielchalmers in #10057
Dialog: Use ParameterState, make DialogOptions immutable by @ScarletKuro in #10066
Inputs, Menu: Standardize default origin point by @danielchalmers in #10071
MudDrawer: Make Temporary Drawer non-responsive by @Nickztar in #10095
MudDataGrid: Rename _classname to Classname etc by @ScarletKuro in #10149
MudPagination: Use ParameterState by @ScarletKuro in #10154
MudBreadcrumbs: Convert BreadcrumbItem to record type by @danielchalmers in #10116
MudBreadcrumbs: Change HTML structure and semantics for accessibility by @danielchalmers in #10115
MudToggleGroup: Fix icon margins by @danielchalmers in #10162
MudSelect: Open menu on pointer down instead of click by @danielchalmers in #10129
MudAutocomplete: Don't wrap around selected index with down arrow by @danielchalmers in #10161
MudAutocomplete: Coerce value immediately by @vernou in #10138
MudSwipeArea: Replace ontouch with onpointer by @ScarletKuro in #9445
Snackbar: Allow to attach custom task on close button click by @BieleckiLtd in #8589
MudColor: Change Equals logic by @ScarletKuro in #10355
MudSelect: Change Visibility of "_currentIcon" to Internal by @jperson2000 in #10451
MudAutocomplete: Fix overlay by @versile2 in #10446
Menu: Allow simplified submenu syntax by @danielchalmers in #10469
MudMenuItem: Decouple from MudListItem and Change Icon Layout by @danielchalmers in #10478
MudToggleGroup: Remove `Rounded` in favor of CSS utilities `rounded-*` by @danielchalmers in #10533
MudChip: Use anchor or button tag instead of div when appropriate by @danielchalmers in #10488
MudGlobal: Remove low impact properties by @danielchalmers in #10516
Inputs: Improve DropdownSettings and RelativeWidth by @versile2 in #10565
New Features
ParameterState: Small optimization & using cleanup by @ScarletKuro in #9955
MudGlobal: Add more component defaults by @swatkinson in #9691
EventListener: Use [JsonSourceGenerationOptions] & cleanup by @ScarletKuro in #9983
BrowserViewportService: Add CancellationTokenSource by @ScarletKuro in #9990
PopoverService: Improve DisposeAsync by @ScarletKuro in #9991
KeyInterceptorService: Improve DisposeAsync by @ScarletKuro in #9992
DialogService: Mark sync API as obsolete by @ScarletKuro in #10000
ObserverManager: Use ConcurrentDictionary by @ScarletKuro in #10023
DataGrid: Allow using enum with Display attributes for filter #7621 by @TheCrazyWolf in #8581
JsEvent: Use InvokeVoidAsyncWithErrorHandling by @ScarletKuro in #10025
MudDataGrid: Accessibility fixes by @OllieFox131 in #10044
MudStepper: Add opt-out Ripple property by @danielchalmers in #10060
MudAutocomplete: add `InputClass` by @danielchalmers in #10064
MudGlobal: Add more defaults for Button and Input by @sidneyxvr in #10006
MudDataGrid Validator [Parameter] (#9860) by @candritzkynet in #10065
MudDatePicker: Use TimeProvider to allow mocking current date by @emcbem in #10087
Inputs: Hide Clear button when readonly by @danielchalmers in #10070
Picker: Add nullable annotation by @meenzen in #10121
MudMenu: Fix PositionAtCursor by @versile2 in #10122
Form: Add nullable annotation by @meenzen in #10125
MudTreeView: Add parameter FilterFunc for selectively displaying tree nodes by @mueller-marcel in #10096
MudNavGroup: Add TitleContent parameter by @Ben0421 in #10160
MudDataGrid: Custom comparer by @Anu6is in #10156
Autocomplete: Add nullable annotation by @meenzen in #10127
MudInputAdornment: Add nullable annotation. by @ScarletKuro in #10203
Mask: Add nullable annotation by @meenzen in #10119
NumericField: Add nullable annotation by @meenzen in #10123
Input: Add nullable annotation by @meenzen in #10124
MudNumericField: Change default InputMode to decimal by @Eddie-Hartman in #9923
MudAppBar: Add contextual action bar implementation by @Anu6is in #10210
Add .NET9 Support by @ScarletKuro in #10233
MudGlobal: Add defaults for MudPaper by @rena0157 in #10264
MudColor: Add Lerp by @ScarletKuro in #10275
MudColor: Add IFormattable by @ScarletKuro in #10295
MudNavGroup: Add HeaderClass for title customization by @Anu6is in #10268
MudSnackbar: Move Icon configs to CommonSnackbarOptions by @Anu6is in #10285
MudColor: Add Palette algorithms by @ScarletKuro in #10313
MudColor: Add GenerateMultiGradientPalette by @ScarletKuro in #10318
MudDataGrid: Allow overriding default filter operators per column by @Anu6is in #10254
MudColorPicker: Add nullable annotation. by @ScarletKuro in #10321
MudColor: Add IParsable and Deconstruct by @ScarletKuro in #10333
New Chart: HeatMap by @versile2 in #10263
MudAutocomplete, MudSelect: Allow popover customization and add scroll events by @versile2 in #10327
MudMenu: Hide Button when Label, Icon and Content are unset by @meenzen in #10349
ParameterState: Add IsChildOriginatedChange to ParameterChangedEventArgs by @ScarletKuro in #10359
MudDataGrid: Add AggregateTemplate render fragment by @Anu6is in #10419
Menu: Improve nesting support by @danielchalmers in #10452
Menu: Make `Open` a two-way parameter by @danielchalmers in #10459
MenuItem: Add `Label` parameter by @danielchalmers in #10476
MudDataGrid: Add CurrentPageChanged EventHandler to enable binding by @ZizWing in #10483
MudTable: Add CurrentPageChanged fot two-way binding (#3563 #6260) by @ZizWing in #10458
MudMenuItem: Add chevrons to submenus and automatically choose anchor origin by @danielchalmers in #10509
MudMenu: Open standard sub menus with left click by @danielchalmers in #10539
MudMenu: Bring style closer to Material Design by @danielchalmers in #10542
MudMenu: Improve usage of `Dense`, icons, and touch by @danielchalmers in #10545
MudChart: HeatMap Cell Customization and Override by @versile2 in #10365
MudSelect: Enhance Keyboard Searching by @Anu6is in #10433
Converter: Enable translation for error messages (#3313) by @pwasilewski in #10411
Updated analyzer for v8 by @peterthorpe81 in #10644
Bug Fixes
MudTheme: Fix STJ Deserialization & add test by @ScarletKuro in #9961
MudTabs: Fix ElementReference regression in #9889 by @ScarletKuro in #9970
MudSelect: Disable logging for KeyInterceptor by @ScarletKuro in #9977
MudStepper: Fix some bugs and improve documentation by @henon in #9989
Popover: Hide all element overflow by @danielchalmers in #9993
MudChart: Add null check for InputData (#10007) by @dunds-com in #10008
BrowserViewportService: Improve CancellationToken logic by @ScarletKuro in #10012
PopoverService: Improve CancellationToken logic by @ScarletKuro in #10013
BrowserViewportService: Remove semaphore, Drawer: Use IAsyncDisposable by @ScarletKuro in #10020
Table: Avoid checkbox overflow by @danielchalmers in #9914
MudAutocomplete : CoerceValue only on Blur with Immediate=false by @vernou in #9881
EventUtil: Fix UnsafeAccessor by @ScarletKuro in #10043
MudDataGrid: Fix regression in #9916 and #9801 by @ScarletKuro in #10042
MudMenu: Fix nested menu to support multiple levels. (#7376) by @charles7668 in #9537
Inputs: Restore original line height by @danielchalmers in #10059
MudTabs, MudDataGrid: Fix memory leaks #9735 by @ScarletKuro in #10062
MudStepper: Fix CompletedContent not shown in Vertical variant by @henon in #10075
MudAutocomplete: Fix Text coersion on Tab key (OnBlur or CloseMenu) by @HClausing in #10074
MudAutocomplete: Fix ResetAsync without debounce not opening the menu by @vernou in #10063
MudPopover: Fix z-index issues with nested popovers by @versile2 in #10089
MudPopover: Fix regression with respect to direction and location and z-index by @versile2 in #10106
Fix origin points that relied on previous defaults by @danielchalmers in #10110
MudToggleGroup: Unregister removed items by @danielchalmers in #10091
MudBreadcrumbs: Fix separator template not vertically centered by @danielchalmers in #10112
MudDataGrid: Correctly render initially expanded rows by @Anu6is in #10133
IJSRuntimeExtensions: Fix when pre-rendering by @ScarletKuro in #10142
MudTextField: Fix ShrinkLabel for text fields with Mask by @Anu6is in #10134
MudDataGrid: Fix Sticky Columns with Sticky Headers by @versile2 in #10151
JS: Fix cannot read properties of undefined / null by @ScarletKuro in #10202
MudList: Fix logic of the Gutters parameter by @henon in #10199
MudTabs: Fix scrolling to active index by @timmac-qmc in #10184
MudDataGrid: Allow grouping by null values by @ScarletKuro in #10213
[MudSelect] Control popover width by @Anu6is in #10215
MudSelect, MudMenu: Fix max height exceeds window by @versile2 in #10216
MudSelect/MudDatePicker: Fix accidental MudDatePicker break while maintaining desired MudSelect behavior by @Anu6is in #10238
MudDataGrid: Fix virtualized indexes (#10179) by @Ptipoi-jf in #10218
MudMenu: Fix menu closing if mouse move too quickly by @charles7668 in #10172
DataGridTests: Fix test failure by using Invariant Culture by @meenzen in #10220
Pickers: Fix popover elevation and gap by @danielchalmers in #10270
Revert "Popover: Hide all element overflow" by @danielchalmers in #10308
MudDialog: Fix exception when closing a dialog while rendering by @vernou in #10226
MudColor: Fix special case in GenerateGradientPalette by @ScarletKuro in #10316
MudDataGrid: Fix Filter Panel Positioning with FixedHeader = true by @versile2 in #10292
MudTimeline: Fix item alignment in nested horizontal timeline (#9958) by @charles7668 in #10346
IJSRuntimeExtensions: Fix InvokeVoidAsyncIgnoreErrors to catch JSException by @brpeeters in #10356
Cookie Consent CSS: Fixes the Cookie Consent Popup from Blocking the UI by @mouse0270 in #10368
MudDatePicker: Fix bugs in FixYear and FixMonth by @charles7668 in #10392
DateMask: Fix leap year February logic 02/29 by @charles7668 in #10377
MudTextField: With mask, setting parameter value updates the input text by @vernou in #10328
MudDataGrid: Fix virtualization regression (#10343) by @Ptipoi-jf in #10402
MudDataGrid: Fix SelectOnRowClick when MultiSelection is false by @Anu6is in #10387
Build: Better sasscompiler workaround by @mikes-gh in #10417
MudDataGrid: Fix Resizable property behavior by @Anu6is in #10436
MudDataGrid: Don't remove `Is Not Empty` filter definition on filter close by @Anu6is in #10438
MudSelect: Prevent adornment `pointerdown` propogating to input by @danielchalmers in #10353
Fixing MudRatingItem ADA issue by @ocastanedaglobant in #10429
MudDateRangePicker: Fix #9667: Fix Clearable Button Not Disappearing with No Range Data by @charles7668 in #10401
MudSelect: Fix MultiSelection in MudSelect by @charles7668 in #10395
MudTextField: Fix MudTextField label background by @charles7668 in #10385
MudTextField: Fix clickable area in multiline fields by @charles7668 in #10364
Menu: Optimize pointer events by @danielchalmers in #10479
MudDataGrid: Align select column and row select behavior by @Anu6is in #10475
MudNumericField: Use native html input number type by @Anu6is in #10192
MudDrawer: Fix bug related to Z-Index and Overlay by @versile2 in #10467
MudAutocomplete: Fix OnAdornmentClick regression #10446 by @versile2 in #10500
MudDataGrid: Fix regression ServerData + Virtualize flag (#10495) & Footer columns (#10501) by @Ptipoi-jf in #10507
MudDataGrid: Fix the shifting of striped rows when loading by @BieleckiLtd in #10528
MudMenu: Fix pointer event timing with cascading menus by @danielchalmers in #10551
MudPopover: Fix overlay regression in nested popovers inside Dialog by @versile2 in #10554
MudStepper: ResetAsync now sends correct StepAction arg by @Skuzzle-UK in #10341
MudSwipeArea: Fix onpointer on touch devices by @ScarletKuro in #10461
MudToggleGroup: Reorder Selection Logic for ToggleGroupItems by @versile2 in #10544
HeatMap: Use invariant culture by @versile2 in #10597
MudDataGrid: Fix no records found appearance with Virtualize by @Ptipoi-jf in #10552
MudTextField: Fix Outlined text field variant label in RTL by @Anu6is in #10642
Other Changes
StringExtensions: Remove not used API & add tests by @ScarletKuro in #9984
TimeSpanExtensions: Remove not used API & add tests by @ScarletKuro in #9987
Localization: Remove IStringLocalizer from InternalMudLocalizer by @ScarletKuro in #10024
MudStepper: Use nameof() instead of hardcoded strings by @0xced in #10079
Refactor: Remove CodeMessage by @ScarletKuro in #10102
Refactor: Fix formatting by @ScarletKuro in #10105
Test Viewer: UI/UX Refresh by @danielchalmers in #10111
Chore: Drop CollectionExtensions for .NET 9+ by @xC0dex in #10165
Fix`--mud-appbar-height` not always match `MudAppBar`'s actual height by @RamType0 in #10097
Revert "Fix`--mud-appbar-height` not always match `MudAppBar`'s actual height" by @danielchalmers in #10214
MudInput: Remove `@onmousewheel` by @ScarletKuro in #10221
README: Update support policy by @danielchalmers in #10445
New Component - MudChat by @versile2 in #10400
MudMenu: Small optimization by @ScarletKuro in #10466
Refactor: Cleanup some console messages by @mikes-gh in #10464
CSS: Remove `ul` from base reset by @danielchalmers in #10611
New Contributors
@swatkinson made their first contribution in #9691
@dunds-com made their first contribution in #10008
@TheCrazyWolf made their first contribution in #8581
@charles7668 made their first contribution in #9537
@OllieFox131 made their first contribution in #10044
@sidneyxvr made their first contribution in #10006
@candritzkynet made their first contribution in #10065
@emcbem made their first contribution in #10087
@timmac-qmc made their first contribution in #10184
@RamType0 made their first contribution in #10097
@Ptipoi-jf made their first contribution in #10218
@brpeeters made their first contribution in #10356
@mouse0270 made their first contribution in #10368
@ocastanedaglobant made their first contribution in #10429
@ZizWing made their first contribution in #10483
@Skuzzle-UK made their first contribution in #10341
@pwasilewski made their first contribution in #10411
Full Changelog: v7.9.0...v8.0.0

Version v7.16.0
Released on Ocak 17, 2025

What's Changed
Bug Fixes
MudDataGrid: Backport #10436 #10220 by @CoreyHayward in #10647
New Contributors
@CoreyHayward made their first contribution in #10647
Full Changelog: v7.15.0...v7.16.0

Version v8.0.0-rc.2
Released on Ocak 09, 2025

Note
This is a re-release of `v8.0.0-rc.1` where `MudBlazor.min.css` was missing.
What's Changed
Breaking Changes
MudToggleGroup: Remove `Rounded` in favor of CSS utilities `rounded-*` by @danielchalmers in #10533
MudChip: Use anchor or button tag instead of div when appropriate by @danielchalmers in #10488
MudGlobal: Remove low impact properties by @danielchalmers in #10516
New Features
MudMenuItem: Add chevrons to submenus and automatically choose anchor origin by @danielchalmers in #10509
MudMenu: Open standard sub menus with left click by @danielchalmers in #10539
MudMenu: Bring style closer to Material Design by @danielchalmers in #10542
MudMenu: Improve usage of `Dense`, icons, and touch by @danielchalmers in #10545
MudChart: HeatMap Cell Customization and Override by @versile2 in #10365
MudSelect: Enhance Keyboard Searching by @Anu6is in #10433
Converter: Enable translation for error messages (#3313) by @pwasilewski in #10411
Bug Fixes
MudDataGrid: Fix the shifting of striped rows when loading by @BieleckiLtd in #10528
MudMenu: Fix pointer event timing with cascading menus by @danielchalmers in #10551
MudPopover: Fix overlay regression in nested popovers inside Dialog by @versile2 in #10554
MudStepper: ResetAsync now sends correct StepAction arg by @Skuzzle-UK in #10341
MudSwipeArea: Fix onpointer on touch devices by @ScarletKuro in #10461
Revert "Build: Sass compiler file contention fix (#10564)" by @mikes-gh in #10589
New Contributors
@Skuzzle-UK made their first contribution in #10341
@pwasilewski made their first contribution in #10411
Full Changelog: v8.0.0-preview.7...v8.0.0-rc.2

Version v8.0.0-preview.7
Released on Aralık 26, 2024

What's Changed
Breaking Changes
Menu: Allow simplified submenu syntax by @danielchalmers in #10469
MudMenuItem: Decouple from MudListItem and Change Icon Layout by @danielchalmers in #10478
New Features
Menu: Make `Open` a two-way parameter by @danielchalmers in #10459
MenuItem: Add `Label` parameter by @danielchalmers in #10476
MudDataGrid: Add CurrentPageChanged EventHandler to enable binding by @ZizWing in #10483
MudTable: Add CurrentPageChanged fot two-way binding (#3563 #6260) by @ZizWing in #10458
Bug Fixes
Menu: Optimize pointer events by @danielchalmers in #10479
MudDataGrid: Align select column and row select behavior by @Anu6is in #10475
MudNumericField: Use native html input number type by @Anu6is in #10192
MudDrawer: Fix bug related to Z-Index and Overlay by @versile2 in #10467
MudAutocomplete: Fix OnAdornmentClick regression #10446 by @versile2 in #10500
MudDataGrid: Fix regression ServerData + Virtualize flag (#10495) & Footer columns (#10501) by @Ptipoi-jf in #10507
Other Changes
MudMenu: Small optimization by @ScarletKuro in #10466
Refactor: Cleanup some console messages by @mikes-gh in #10464
New Contributors
@ZizWing made their first contribution in #10483
Full Changelog: v8.0.0-preview.6...v8.0.0-preview.7

Version v8.0.0-preview.6
Released on Aralık 17, 2024

What's Changed
Breaking Changes
Snackbar: Allow to attach custom task on close button click by @BieleckiLtd in #8589
MudColor: Change Equals logic by @ScarletKuro in #10355
MudSelect: Change Visibility of "_currentIcon" to Internal by @jperson2000 in #10451
MudAutocomplete: Fix overlay by @versile2 in #10446
New Features
MudColor: Add Palette algorithms by @ScarletKuro in #10313
MudColor: Add GenerateMultiGradientPalette by @ScarletKuro in #10318
MudDataGrid: Allow overriding default filter operators per column by @Anu6is in #10254
MudColorPicker: Add nullable annotation. by @ScarletKuro in #10321
MudColor: Add IParsable and Deconstruct by @ScarletKuro in #10333
New Chart: HeatMap by @versile2 in #10263
MudAutocomplete, MudSelect: Allow popover customization and add scroll events by @versile2 in #10327
MudMenu: Hide Button when Label, Icon and Content are unset by @meenzen in #10349
ParameterState: Add IsChildOriginatedChange to ParameterChangedEventArgs by @ScarletKuro in #10359
MudDataGrid: Add AggregateTemplate render fragment by @Anu6is in #10419
Menu: Improve nesting support by @danielchalmers in #10452
Bug Fixes
MudDialog: Fix exception when closing a dialog while rendering by @vernou in #10226
MudColor: Fix special case in GenerateGradientPalette by @ScarletKuro in #10316
MudDataGrid: Fix Filter Panel Positioning with FixedHeader = true by @versile2 in #10292
MudTimeline: Fix item alignment in nested horizontal timeline (#9958) by @charles7668 in #10346
IJSRuntimeExtensions: Fix InvokeVoidAsyncIgnoreErrors to catch JSException by @brpeeters in #10356
Cookie Consent CSS: Fixes the Cookie Consent Popup from Blocking the UI by @mouse0270 in #10368
MudDatePicker: Fix bugs in FixYear and FixMonth by @charles7668 in #10392
DateMask: Fix leap year February logic 02/29 by @charles7668 in #10377
MudTextField: With mask, setting parameter value updates the input text by @vernou in #10328
MudDataGrid: Fix virtualization regression (#10343) by @Ptipoi-jf in #10402
MudDataGrid: Fix SelectOnRowClick when MultiSelection is false by @Anu6is in #10387
Build: Better sasscompiler workaround by @mikes-gh in #10417
MudDataGrid: Fix Resizable property behavior by @Anu6is in #10436
MudDataGrid: Don't remove `Is Not Empty` filter definition on filter close by @Anu6is in #10438
MudSelect: Prevent adornment `pointerdown` propogating to input by @danielchalmers in #10353
Fixing MudRatingItem ADA issue by @ocastanedaglobant in #10429
MudDateRangePicker: Fix #9667: Fix Clearable Button Not Disappearing with No Range Data by @charles7668 in #10401
MudSelect: Fix MultiSelection in MudSelect by @charles7668 in #10395
MudTextField: Fix MudTextField label background by @charles7668 in #10385
MudTextField: Fix clickable area in multiline fields by @charles7668 in #10364
Other Changes
README: Update support policy by @danielchalmers in #10445
New Component - MudChat by @versile2 in #10400
New Contributors
@brpeeters made their first contribution in #10356
@mouse0270 made their first contribution in #10368
@ocastanedaglobant made their first contribution in #10429
Full Changelog: v8.0.0-preview.5...v8.0.0-preview.6

Version v8.0.0-preview.5
Released on Kasım 22, 2024

What's Changed
Breaking Changes
MudDataGrid: Rename _classname to Classname etc by @ScarletKuro in #10149
MudPagination: Use ParameterState by @ScarletKuro in #10154
MudBreadcrumbs: Convert BreadcrumbItem to record type by @danielchalmers in #10116
MudBreadcrumbs: Change HTML structure and semantics for accessibility by @danielchalmers in #10115
MudToggleGroup: Fix icon margins by @danielchalmers in #10162
MudSelect: Open menu on pointer down instead of click by @danielchalmers in #10129
MudAutocomplete: Don't wrap around selected index with down arrow by @danielchalmers in #10161
MudAutocomplete: Coerce value immediately by @vernou in #10138
MudSwipeArea: Replace ontouch with onpointer by @ScarletKuro in #9445
New Features
MudTreeView: Add parameter FilterFunc for selectively displaying tree nodes by @mueller-marcel in #10096
MudNavGroup: Add TitleContent parameter by @Ben0421 in #10160
MudDataGrid: Custom comparer by @Anu6is in #10156
Autocomplete: Add nullable annotation by @meenzen in #10127
MudInputAdornment: Add nullable annotation. by @ScarletKuro in #10203
Mask: Add nullable annotation by @meenzen in #10119
NumericField: Add nullable annotation by @meenzen in #10123
Input: Add nullable annotation by @meenzen in #10124
MudNumericField: Change default InputMode to decimal by @Eddie-Hartman in #9923
MudAppBar: Add contextual action bar implementation by @Anu6is in #10210
Add .NET9 Support by @ScarletKuro in #10233
MudGlobal: Add defaults for MudPaper by @rena0157 in #10264
MudColor: Add Lerp by @ScarletKuro in #10275
MudColor: Add IFormattable by @ScarletKuro in #10295
MudNavGroup: Add HeaderClass for title customization by @Anu6is in #10268
Snackbar: Move Icon configs to CommonSnackbarOptions by @Anu6is in #10285
Bug Fixes
MudTextField: Fix ShrinkLabel for text fields with Mask by @Anu6is in #10134
MudDataGrid: Fix Sticky Columns with Sticky Headers by @versile2 in #10151
JS: Fix cannot read properties of undefined / null by @ScarletKuro in #10202
MudList: Fix logic of the Gutters parameter by @henon in #10199
MudTabs: Fix scrolling to active index by @timmac-qmc in #10184
MudDataGrid: Allow grouping by null values by @ScarletKuro in #10213
[MudSelect] Control popover width by @Anu6is in #10215
MudSelect, MudMenu: Fix max height exceeds window by @versile2 in #10216
MudSelect/MudDatePicker: Fix accidental MudDatePicker break while maintaining desired MudSelect behavior by @Anu6is in #10238
MudDataGrid: Fix virtualized indexes (#10179) by @Ptipoi-jf in #10218
MudMenu: Fix menu closing if mouse move too quickly by @charles7668 in #10172
DataGridTests: Fix test failure by using Invariant Culture by @meenzen in #10220
Pickers: Fix popover elevation and gap by @danielchalmers in #10270
Revert "Popover: Hide all element overflow" by @danielchalmers in #10308
Other Changes
Chore: Drop CollectionExtensions for .NET 9+ by @xC0dex in #10165
MudInput: Remove `@onmousewheel` by @ScarletKuro in #10221
New Contributors
@timmac-qmc made their first contribution in #10184
@RamType0 made their first contribution in #10097
@Ptipoi-jf made their first contribution in #10218
Full Changelog: v8.0.0-preview.4...v8.0.0-preview.5

Version v8.0.0-preview.4
Released on Ekim 30, 2024

What's Changed
New Features
Picker: Add nullable annotation by @meenzen in #10121
MudMenu: Fix PositionAtCursor by @versile2 in #10122
Form: Add nullable annotation by @meenzen in #10125
Bug Fixes
MudBreadcrumbs: Fix separator template not vertically centered by @danielchalmers in #10112
MudDataGrid: Correctly render initially expanded rows by @Anu6is in #10133
IJSRuntimeExtensions: Fix when pre-rendering by @ScarletKuro in #10142
Full Changelog: v8.0.0-preview.3...v8.0.0-preview.4

Version v7.15.0
Released on Ekim 29, 2024

What's Changed
New Features
MudGlobal: Add more component defaults by @swatkinson in #9691
MudGlobal: MudGlobal: Add more defaults for Button and Input by @sidneyxvr in #10006
Other Changes
Docs: Fix Releases page not linking PR IDs other than 4 in length by @danielchalmers in #10118
Full Changelog: v7.14.0...v7.15.0

Version v8.0.0-preview.3
Released on Ekim 28, 2024

What's Changed
Breaking Changes
Inputs, Menu: Standardize default origin point by @danielchalmers in #10071
MudDrawer: Make Temporary Drawer non-responsive by @Nickztar in #10095
New Features
MudDatePicker: Use TimeProvider to allow mocking current date by @emcbem in #10087
Inputs: Hide Clear button when readonly by @danielchalmers in #10070
Bug Fixes
MudAutocomplete: Fix Text coersion on Tab key (OnBlur or CloseMenu) by @HClausing in #10074
MudAutocomplete: Fix ResetAsync without debounce not opening the menu by @vernou in #10063
MudPopover: Fix z-index issues with nested popovers by @versile2 in #10089
MudPopover: Fix regression with respect to direction and location and z-index by @versile2 in #10106
Fix origin points that relied on previous defaults by @danielchalmers in #10110
MudToggleGroup: Unregister removed items by @danielchalmers in #10091
Other Changes
MudStepper: Use nameof() instead of hardcoded strings by @0xced in #10079
New Contributors
@emcbem made their first contribution in #10087
Full Changelog: v8.0.0-preview.2...v8.0.0-preview.3

Version v8.0.0-preview.2
Released on Ekim 22, 2024

What's Changed
Breaking Changes
MudDialog: Don't override `Class` param and convert BackgroundClass to getter by @henon in #9909
MudFileUpload: fix `AppendMultipleFiles` keeping stale file references (#9586) by @igotinfected in #9600
Typography: Remove `input` in favor of `subtitle1` by @danielchalmers in #10028
Components: Use IAsyncDisposable by @ScarletKuro in #10037
MudSelectItem, MudNavLink: Remove MudBaseSelectItem by @ScarletKuro in #10045
Radio, Check Box, Switch: Unification and improved alignaments by @ralvarezing in #9472
Scroll services: Clean up, make implementations internal, add xmodoc by @ScarletKuro in #10048
EventListener: Clean up, make implementations internal, add xmodoc by @ScarletKuro in #10051
ResizeObserver: Clean up, make implementations internal, add xmodoc by @ScarletKuro in #10055
JsEvent: Use IAsyncDisposable by @ScarletKuro in #10061
MudCollapse: Rework animation by @danielchalmers in #10056
Inputs: Refactor adornment related code by @danielchalmers in #10057
Dialog: Use ParameterState, make DialogOptions immutable by @ScarletKuro in #10066
New Features
ObserverManager: Use ConcurrentDictionary by @ScarletKuro in #10023
DataGrid: Allow using enum with Display attributes for filter #7621 by @TheCrazyWolf in #8581
JsEvent: Use InvokeVoidAsyncWithErrorHandling by @ScarletKuro in #10025
MudDataGrid: Accessibility fixes by @OllieFox131 in #10044
MudStepper: Add opt-out Ripple property by @danielchalmers in #10060
MudAutocomplete: add `InputClass` by @danielchalmers in #10064
MudGlobal: Add more defaults for Button and Input by @sidneyxvr in #10006
MudDataGrid Validator [Parameter] (#9860) by @candritzkynet in #10065
Bug Fixes
MudChart: Add null check for InputData (#10007) by @dunds-com in #10008
BrowserViewportService: Improve CancellationToken logic by @ScarletKuro in #10012
PopoverService: Improve CancellationToken logic by @ScarletKuro in #10013
BrowserViewportService: Remove semaphore, Drawer: Use IAsyncDisposable by @ScarletKuro in #10020
Table: Avoid checkbox overflow by @danielchalmers in #9914
MudAutocomplete : CoerceValue only on Blur with Immediate=false by @vernou in #9881
EventUtil: Fix UnsafeAccessor by @ScarletKuro in #10043
MudDataGrid: Fix regression in #9916 and #9801 by @ScarletKuro in #10042
MudMenu: Fix nested menu to support multiple levels. (#7376) by @charles7668 in #9537
Inputs: Restore original line height by @danielchalmers in #10059
MudTabs, MudDataGrid: Fix memory leaks #9735 by @ScarletKuro in #10062
MudStepper: Fix CompletedContent not shown in Vertical variant by @henon in #10075
Other Changes
Localization: Remove IStringLocalizer from InternalMudLocalizer by @ScarletKuro in #10024
New Contributors
@dunds-com made their first contribution in #10008
@TheCrazyWolf made their first contribution in #8581
@charles7668 made their first contribution in #9537
@OllieFox131 made their first contribution in #10044
@sidneyxvr made their first contribution in #10006
@candritzkynet made their first contribution in #10065
Full Changelog: v8.0.0-preview.1...v8.0.0-preview.2

Version v7.14.0
Released on Ekim 22, 2024

What's Changed
New Features
MudDataGrid: MudDataGrid Validator [Parameter] by @candritzkynet in #10065
Bug Fixes
MudStepper: Fix CompletedContent not shown in Vertical variant by @henon in #10075
Full Changelog: v7.13.0...v7.14.0

Version v7.13.0
Released on Ekim 17, 2024

What's Changed
Bug Fixes
MudDataGrid: Fix regression in #9916 and #9801 by @ScarletKuro in #10042
BrowserViewportService: Remove semaphore, Drawer: Use IAsyncDisposable by @ScarletKuro in #10020 (from v7.12.1)
Full Changelog: v7.12.0...v7.13.0

Version v7.12.0
Released on Ekim 14, 2024

What's Changed
Bug Fixes
PopoverService: Improve CancellationToken logic by @ScarletKuro in #10013
MudChart: Add null check for InputData by @dunds-com in #10008
BrowserViewportService: Improve CancellationToken logic by @ScarletKuro in #10012
Full Changelog: v7.11.0...v7.12.0

Version v8.0.0-preview.1
Released on Ekim 13, 2024

What's Changed
Breaking Changes
Build: Drop support for .NET 7 by @danielchalmers in #9511
Theme: Change Typography members to type BaseTypography by @danielchalmers in #9434
MudTable: Rename MudBlazorFix namespace to MudBlazor by @ScarletKuro in #9952
Rename utilities to fix typos by @danielchalmers in #9582
Typography: Make `FontWeight` and `LineHeight` strings by @danielchalmers in #9592
MudGlobal: Remove `EnableIllegalRazorParameterDetection` by @danielchalmers in #9580
Remove KeyInterceptor, Replaced by KeyInterceptorService by @ScarletKuro in #9956
Remove MudPopoverService by @ScarletKuro in #9957
Key handling followup work by @danielchalmers in #9921
KeyInterceptorService: Make KeyOptions immutable by @ScarletKuro in #9969
DialogService: Remove obsolete OnDialogInstanceAdded by @ScarletKuro in #9980
MudDropContainer: Remove obsolete API by @ScarletKuro in #9981
MudDataGrid: Remove obsolete CancelledEditingItem by @ScarletKuro in #9982
JsApiService: Make implementation internal & add xmldoc. by @ScarletKuro in #9994
JsEvent: Make implementation internal & add xmldoc. by @ScarletKuro in #9996
IJSRuntimeExtensions: Adjust pre-rendering handling for .NET8 by @ScarletKuro in #9997
Localization: Remove obsolete code by @meenzen in #10001
EventUtil: Propagate exceptions. by @ScarletKuro in #9967
TimeSeries: Rename `TimeSeriesDiplayType` to fix typo by @danielchalmers in #9581
MudCharts: Update Namespace, set some classes visibility to internal by @ingkor in #9919
Add `Typography` suffix to all typography classes by @danielchalmers in #9962
New Features
ParameterState: Small optimization & using cleanup by @ScarletKuro in #9955
MudGlobal: Add more component defaults by @swatkinson in #9691
EventListener: Use [JsonSourceGenerationOptions] & cleanup by @ScarletKuro in #9983
BrowserViewportService: Add CancellationTokenSource by @ScarletKuro in #9990
PopoverService: Improve DisposeAsync by @ScarletKuro in #9991
KeyInterceptorService: Improve DisposeAsync by @ScarletKuro in #9992
DialogService: Mark sync API as obsolete by @ScarletKuro in #10000
Bug Fixes
MudTheme: Fix STJ Deserialization & add test by @ScarletKuro in #9961
MudTabs: Fix ElementReference regression in #9889 by @ScarletKuro in #9970
MudSelect: Disable logging for KeyInterceptor by @ScarletKuro in #9977
MudStepper: Fix some bugs and improve documentation by @henon in #9989
Popover: Hide all element overflow by @danielchalmers in #9993
Other Changes
StringExtensions: Remove not used API & add tests by @ScarletKuro in #9984
TimeSpanExtensions: Remove not used API & add tests by @ScarletKuro in #9987
New Contributors
@swatkinson made their first contribution in #9691
Full Changelog: v7.9.0...v8.0.0-preview.1

Version v7.11.0
Released on Ekim 12, 2024

What's Changed
Bug Fixes
MudSelect: Disable logging for KeyInterceptor by @ScarletKuro in #9977
MudStepper: Fix some bugs and improve documentation by @henon in #9989
BrowserViewportService: Add CancellationTokenSource by @ScarletKuro in #9990
Full Changelog: v7.10.0...v7.11.0

Version v7.10.0
Released on Ekim 11, 2024

What's Changed
Bug Fixes
KeyInterceptorOptions: Fix breaking change introduced in #9896 by @ScarletKuro in #9971
MudTabs: Fix ElementReference regression in #9889 by @ScarletKuro in #9970
Full Changelog: v7.9.0...v7.10.0

Version v7.9.0
Released on Ekim 10, 2024

What's Changed
New Features
MudTreeViewItem: Add property Visible for conditionally hiding sub-trees without reloading by @mueller-marcel in #9798
MudButtonGroup: Stretch child buttons by @vernou in #9713
Add dark mode to native html input controls by @digitaldirk in #9814
DateMask: Remove logging in ModifyFinalText by @ScarletKuro in #9861
MudTimeSeriesChart: Add X & Y Axis Title parameters by @MattParkerDev in #9781
MudSkeleton: add dark mode support by @digitaldirk in #9815
MudTabs: Add nullable annotation by @meenzen in #9889
MudTextField: Add nullable annotation. by @ScarletKuro in #9897
Introduce KeyInterceptorService withs sync/async support to replace KeyInterceptor by @ScarletKuro in #9896
MudDialog: Add OnKeyDown and OnKeyUp events by @henon in #9906
MudChart: Add nullable annotation by @meenzen in #9907
MudChip: Improve keyboard accessibility by @igotinfected in #9455
MudStepper: New Component by @henon in #9911
MudExpansionPanel: Add HeaderClass by @MeysamMoghaddam in #6743
MudSelect: Add nullable annotation. by @ScarletKuro in #9937
MudForm: Expose ChildForms to derived classes by @tomasz-soltysik in #7470
Bug Fixes
MudDialogInstance: SetTitle mark parameter as nullable (#9817) by @IIARROWS in #9818
MudListItem: Allow overriding `tabindex` via UserAttributes by @hallaelsa in #9822
MudColor: Remove [JsonInclude] from public properties by @ScarletKuro in #9838
MudButton : Implement IDisposable by @vernou in #9835
MudButton: Fix sonar warning about Dispose implementation by @ScarletKuro in #9844
MudDataGrid: properly clear the filter values by @0xced in #9801
MudListItem: Fix redundant whitespace in CSS class (#9868) by @Eagle3386 in #9869
MudDateRangePicker: Incorrect margins and align when on a stack or grid by @ralvarezing in #9890
MudDataGrid: Fix NRE when property has a nested null value by @ScarletKuro in #9916
MudCheckBox and MudRadio: Do not grey out when read-only (#9876) by @ingkor in #9904
MudAutocomplete: Incorrect horizontal spacing on RTL mode by @ralvarezing in #9913
DataGrid: Prevent applying same filters twice by @Namoshek in #9877
MudDataGrid: Remove empty filters when closing the filter menu without applying (#8998) by @henjoh75 in #9885
MudStepper: Fix step number alignment by @Garderoben in #9931
PopoverService: Remove locking by @ScarletKuro in #9938
MudPopover: Fix negative results in JS calculations for overflow behavior by @versile2 in #9933
New Contributors
@hallaelsa made their first contribution in #9822
@MeysamMoghaddam made their first contribution in #6743
@henjoh75 made their first contribution in #9885
Full Changelog: v7.8.0...v7.9.0

Version v7.8.0
Released on Eylül 06, 2024

What's Changed
Bug Fixes
Snackbar: Revert ReaderWriterLockSlim dispose in SnackbarService by @ScarletKuro in #9763
CsssBuilder/StyleBuilder: Fix for initialized as default by @renaudrenaud85 in #9765
Full Changelog: v7.7.0...v7.8.0

Version v7.7.0
Released on Eylül 02, 2024

What's Changed
New Features
Icons: Add Empty icon by @skyslide22 in #9478
MudTable: Cancel ServerLoad func on Dispose by @boukenka in #9604
MudField: Text overflow ellipsis for labels by @ArcadeMode in #9613
Snackbar: Add nullable annotationby @xC0dex in #9454
Bug Fixes
MudText: Handle empty HtmlTag as null by @danielchalmers in #9576
MudDropContainer: Fixes typos in method names by @danielchalmers in #9578
MudNumericField: Support `Typo` property by @danielchalmers in #9608
Icons: Fix Steam icon by @ScarletKuro in #9641
MudAutocomplete: Fix search not being triggered when the text is cleared by @vernou in #9599
MudDataGrid : Fix Resize column when neighboring is hidden by @vernou in #9643
MudInput: Revert left and right margins added in #9517 by @ralvarezing in #9652
MudRadioGroup: Bind after should not trigger when value is modified externally by @Mr-Technician in #9614
Autocomplete: Improve handling of focus events by @danielchalmers in #9639
MudSelect: Fix OnKeyDown not triggered correctly for the Enter key by @mwiehler in #9595
BrowserViewportService: Fix ResizeOptions by @ScarletKuro in #9686
ToggleGroup: Fix bad layout on initial render by @danielchalmers in #9706
MudCollapse: Remove `aria-expanded` binding by @danielchalmers in #9714
Other Changes
MudAutocomplete: Cancel SearchFunc on Dispose by @boukenka in #9609
New Contributors
@w3ori made their first contribution in #9624
@renaudrenaud85 made their first contribution in #9635
@mwiehler made their first contribution in #9595
@renovate-bot made their first contribution in #9687
Full Changelog: v7.6.0...v7.7.0

Version v7.6.0
Released on Ağustos 06, 2024

What's Changed
New Features
MudThemeProvider: Add ObserveSystemThemeChange by @ScarletKuro in #9442
MudDateRangePicker: Add parameter to allow capture of disabled dates (#9452) by @tavanuka in #9466
MudTreeView: Allow for parent auto selection configuration by @BarbeRouss in #9567
Bug Fixes
DataGrid: Fix multiple filter popups can be opened by @danielchalmers in #9546
MudTimePicker: Fix dispose method by @danielchalmers in #9552
MudElement: Fix ElementReferenceCapture by @danielchalmers in #9547
MudDialog: Avoid using aria-hidden on a focused element or its ancestor by @danielchalmers in #9545
MudInput: Handle AutoGrow dispose when ElementReference is null by @danielchalmers in #9561
Menu: Stay open after being clicked with `MouseOver` by @danielchalmers in #9513
Make object-fit and object-position styles important by @danielchalmers in #9499
Inputs: Fixed margins on each variant and margin sizes by @ralvarezing in #9517
MudTreeViewItem: Add ReadOnly by @FreyLuis in #9557
Other Changes
Alert: Fix vertical icon position on alerts by @gabephudson in #9526
Performance: Use Identifier helper in more cases by @xC0dex in #9558
New Contributors
@gabephudson made their first contribution in #9526
@FreyLuis made their first contribution in #9557
@CSkjolden made their first contribution in #9564
@BarbeRouss made their first contribution in #9567
Full Changelog: v7.5.0...v7.6.0

Version v7.5.0
Released on Temmuz 31, 2024

What's Changed
New Features
MudOverlay: Refactor, Deprecate `OnClick` in favor of new `OnClosed`, Fix MudMenu interactions by @danielchalmers in #9458
MudDrawer: Add OverlayAutoClose, use OnClosed instead of OnClick by @ScarletKuro in #9500
DataGrid: Make FilterContext.FilterDefinition public to support custom filters by @Namoshek in #9522
Localization: Make InternalMudLocalizer implement IStringLocalizer by @ScarletKuro in #9527
Bug Fixes
TreeViewItem: ReloadAsync() now also reloads in collapsed state by @henon in #9503
Other Changes
Performance: Add more extension methods for Array and List by @xC0dex in #9487
Build: Enable implicit usings for all projects by @danielchalmers in #9480
Performance: Add FirstOrDefault extension method for Array and List by @xC0dex in #9515
Performance: Add Identifier helper class by @xC0dex in #9493
New Contributors
@Namoshek made their first contribution in #9522
Full Changelog: v7.4.0...v7.5.0

Version v7.4.0
Released on Temmuz 23, 2024

What's Changed
New Features
MudDialog: Improve max height and alignment of dialog container on mobile by @skyslide22 in #9457
Bug Fixes
Localization: Correctly handle templates (#9438) by @meenzen in #9451
MudDataGrid: Fix `RowsPerPage` firing ServerData twice by @vernou in #9448
TableGrid: Fix RowsPerPage firing ServerData twice by @vernou in #9463
Other Changes
Performance: Add faster Any(...) Extensions for Array and List by @xC0dex in #9461
New Contributors
@Welchen made their first contribution in #9440
Full Changelog: v7.3.0...v7.4.0

Version v7.3.0
Released on Temmuz 17, 2024

What's Changed
New Features
MudPagination: Control show or hide the page button (#5536) by @maikelvanhaaren in #9323
Table, DataGrid: Support localization of pager text by @meenzen in #9370
DialogService: Fix CloseOnEscape in ShowAsync and ShowMessageBox (#9367) by @versile2 in #9375
TreeItemData: Add virtual to all properties as per request by @henon in #9432
MudTable: Add SelectionChangeable parameter by @Ben0421 in #9374
Bug Fixes
MudTreeView: Don't select on expand button double click by @henon in #9428
MudField, MudMask: Support typography customization by @danielchalmers in #9421
Other Changes
Revert "Theme: Change Typography members to type BaseTypography" by @danielchalmers in #9423
IDialogService: Forward event DialogInstanceAddedAsync to OnDialogInstanceAdded for compatibility by @ScarletKuro in #9431
New Contributors
@versile2 made their first contribution in #9375
@Ben0421 made their first contribution in #9374
Full Changelog: v7.2.0...v7.3.0

Version v7.2.0
Released on Temmuz 15, 2024

What's Changed
New Features
Autocomplete: Add opt-out `OpenOnFocus` property by @danielchalmers in #9385
Theme: Change Typography members to type BaseTypography by @danielchalmers in #9368
PopoverService: Add CheckForPopoverProvider by @ScarletKuro in #9391
MudPicker: Add OverflowBehavior that is passed to the underlying MudPopover by @benm-eras in #9372
Inputs: Allow custom Clear icon by @danielchalmers in #9394
MudToggleIconButton: Fall back to regular property if toggled version not set by @danielchalmers in #9398
Bug Fixes
DialogService: Fix thread is not associated with the Dispatcher by @maxwell60701 in #9306
Hook up `Autocomplete.MaxLength` by @danielchalmers in #9364
MudListItem: Use disabled color for SecondaryText when item is disabled by @danielchalmers in #9390
MudFormComponent: Fix validation handling (#9215) by @SKSniperSK in #9386
New Contributors
@maxwell60701 made their first contribution in #9306
@SKSniperSK made their first contribution in #9386
Full Changelog: v7.1.1...v7.2.0

Version v7.1.1
Released on Temmuz 11, 2024

What's Changed
Other Changes
Remove illegal param detector, this time really by @henon in #9373
Note: this should have been removed in v7.1.0 already but wasn't included due to an accident with GitHub's commit squashing.
Full Changelog: v7.1.0...v7.1.1

Version v7.1.0
Released on Temmuz 10, 2024

What's Changed
New Features
MudBaseInput: Add `protected InputElementId` (#9277) by @igotinfected in #9278
MudMessageBox: Add button classes by @danielchalmers in #9293
Translations: Force keys to follow naming conventions by @meenzen in #9321
Localization: Use source generated resources by @meenzen in #9331
MudAutocomplete: Make the autocomplete attribute configurable (#9334) by @Aaron2404 in #9330
Autocomplete: Allow overriding `autocomplete` via UserAttributes by @danielchalmers in #9337
MudTimePicker: Refactor pointer events to support touch dragging by @danielchalmers in #9300
Bug Fixes
MudDataGridPager: When no items, Pager now shows "0-0 of 0" (#9247) by @rmoroz20 in #9282
MudCollapse: Fix `aria-expanded` value by @danielchalmers in #9303
MudAvatarGroup: Fix Max default value behavior (#9267) by @brian-lagerman in #9309
MudBadge: Center content with flex instead of negative margins by @ekrak in #9305
MudFileUpload: Use ParameterState by @ScarletKuro in #9264
DataGrid: Add missing labels for filter value fields in data grid column filter (#9319) by @aditya119 in #9336
MudDataGrid: Fix Column ungroup by @ScarletKuro in #9344
MudInputControl: Fix `HelperTextOnFocus` regression by @igotinfected in #9346
MudAutocomplete: Modify default autocomplete method (#9318) by @Aaron2404 in #9354
Other Changes
ElementReferenceExtensions: Use delegate for retrieving JSRuntime by @ScarletKuro in #9260
MudAlert: Remove `alert` role by @danielchalmers in #9298
ElementReferenceExtensions: Utilize new UnsafeAccessor by @ScarletKuro in #9324
Accessibility: Simplify `aria-label` text by @danielchalmers in #9339
New Contributors
@brian-lagerman made their first contribution in #9309
@ekrak made their first contribution in #9305
@Aaron2404 made their first contribution in #9330
@lymem made their first contribution in #9360
Full Changelog: v7.0.0...v7.1.0

Version v7.0.0
Released on Haziran 29, 2024

> [!NOTE] > If you are migrating from v6.x.x to v7.0.0 please make sure to read the [v7.0.0 Migration Guide](https://github.com/MudBlazor/MudBlazor/issues/8447) !! You can give us your [feedback about v7](https://github.com/MudBlazor/MudBlazor/discussions/8976), it'll be appreciated.
What's Changed
> [!WARNING] > It's now explicitly required to add `MudPopoverProvider`. [#8712](#8712) Before (v6.x.x): ```html ``` After (v7.x.x): ```html ``` For all other changes and warnings please make sure to visit the [v7.0.0 Migration Guide](https://github.com/MudBlazor/MudBlazor/issues/8447)
Alert
- MudAlert: Fix content alignment issue #8734 by @neozhu in #8735 - Alert: Remove AlertTextPosition by @ScarletKuro in #8637
Autocomplete
- Autocomplete: Give async methods Async suffix by @danielchalmers in #8990 - Autocomplete: Treat all ways of closing overlay the same by @danielchalmers in #8914 - Autocomplete: Improve menu behavior by @danielchalmers in #8787 - MudAutocomplete: `ItemDisabledTemplate` and `ItemSelectedTemplate` will now display even if `ItemTemplate` is not defined. by @digitaldirk in #8913 - MudAutocomplete: Open menu on focus, Rename `ToggleMenu` to `ToggleMenuAsync` by @danielchalmers in #8758 - MudAutocomplete: Fix missing margin-top when label is set by @ralvarezing in #8623 - MudAutocomplete: Remove SearchFuncWithCancel, Add CancellationToken to SearchFunc by @jperson2000 in #8490
Avatar
- MudAvatar: Remove Obsolete Image Property by @Anu6is in #8648
Breadcrumbs
- MudBreadcrumbs: Change from List to IReadOnlyList by @danielchalmers in #8439
Buttons
- Buttons: Remove unnecessary `Title`, `AriaLabel` properties by @danielchalmers in #9098 - Buttons: Make Title a base property by @danielchalmers in #8630 - ButtonGroup: Add `FullWidth`, Rename `VerticalAlign` to `Vertical` by @danielchalmers in #8798 - Button: Revert sticky focus style by @danielchalmers in #8767 - Button: Don't apply :focus effect on mobile so it's not sticky by @danielchalmers in #8709 - MudButton: fix focus style button when open dialog by @LiZzeira in #8575
Card
- MudCard: Fix content not filling remaining space by @danielchalmers in #8933
Chip
- MudChipSet: use `SelectionMode` instead of `MultiSelect` and `Mandatory` by @henon in #8722 - Chip: Remove special palette variables by @danielchalmers in #8599 - Chip: Don't apply hover or focus effect if not clickable and allow text selection by @danielchalmers in #8598
Container
- MudContainer: Add `Gutters` property by @danielchalmers in #8934
DataGrid
- MudDataGrid: With VirtualizeServerData, reset StartIndex if beyond TotalItemsCount boundaries. by @KapustinVadim1991 in #9246 - MudDataGrid: Add correct class to the th element in HeaderCell by @Jimmys20 in #9203 - MudDataGrid: Refresh grouping after InvokeServerLoadFunc, ExpandAllGroups, CollapseAllGroups by @MihFig in #9150 - MudDataGrid: Fixed serverData + virtualization bug (#5664) by @KapustinVadim1991 in #9086 - MudDataGrid: Fix column resizing in RTL (#6965) by @thedude61636 in #9073 - MudDataGrid: Removed Unused "Row" Class by @jperson2000 in #9002 - MudDataGrid: Apply Footer/Header Style Funcs by @kev-andrews in #8853 - MudDataGrid: Add ICloneStrategy by @ScarletKuro in #8851 - MudDataGrid: Rename `DisableRowsPerPage` to `PageSizeSelector` by @henon in #8662 - MudDataGrid: Remove obsolete API by @ScarletKuro in #8548 - MudDataGrid: Fix grouping for bound and unbound scenarios using ParameterState by @peterthorpe81 in #8463 - MudDataGridPager: Add AllItemsText Parameter (#9235) by @rmoroz20 in #9239 - DataGrid: Change default values of TemplateColumn by @dennisrahmen in #9142 - DataGrid: ServerData with Virtualization by @KapustinVadim1991 in #9019 - DataGrid: Remove 'Value not null' criteria for FilterDefinition by @aditya119 in #8706 - DataGrid : Fix sorting when there is Header row (#7645) by @timlunev in #8504 - Datagrid: Add NumberFormat and NumberCulture to AggregateDefinition (#9112) by @alexkleinwaechter in #9120 - Datagrid: Add hierarchycolumn InitiallyExpandedFunc (#8103) by @Nephim - MudDataGrid: Add Hide Parameters to HierarchyColumn and SelectColumn (#8132) by @3x9equals27
Dialog
- MudDialog: Add nullable annotation by @xC0dex in #9146 - MudDialog: Fix content not stretched in fullscreen mode by @danielchalmers in #8743 - MudDialog: Fix some params not passed when inline by @danielchalmers in #8424 - Dialog: Revert #8964 (Fix possible deadlock) by @danielchalmers in #9030 - Dialog: Fix possible deadlock by @ScarletKuro in #8964 - Dialog: Fix visibility of inline dialogs using Show method by @danielchalmers in #8925 - Dialog: Rename ClassContent and ClassActions parameters by @ArieGato in #8481 - MudMessageBox: Use ParameterState framework by @ScarletKuro in #9014 - MudMessageBox: Fix `visible` should be `Visible` by @danielchalmers in #9010 - MudDialogProvider: Add missing BackgroundClass (#8454) by @Alerinos in #8458
Drawer
- MudDrawer: Improve breakpoint logic by @ScarletKuro in #8941 - MudDrawer: Fix initialization behaviour, use ParameterState, remove PreserveOpenState by @ScarletKuro in #8833
ExpansionPanels
- MudExpansionPanels: Rename `DisableBorders` to `Outlined` by @henon in #8593 - MudExpansionPanel: Use ParameterState, remove obsolete API and change to async API by @ScarletKuro in #8446
FileUpload
- FileUpload: fix issue with file picker not opening in Safari with Blazor Server (#9148) by @igotinfected in #9157 - FileUpload: fix uploading same file before and after clearing the input on chromium browsers (#9082) by @igotinfected in #9139 - FileUpload: Rewrite using `ActivatorContent` pattern by @igotinfected in #8694 - FileUpload: Fix top margin CSS by @danielchalmers in #8438 - MudFileUpload: Make FileUploadButtonTemplateContext.ClearAsync required by @ScarletKuro in #8547
Form & Inputs
- MudFormComponent: call base OnParametersSet by @ScarletKuro in #9225 - MudBaseInput components: add helper text reference to `aria-describedby` attribute by @igotinfected in #9193 - MudSwitch: Add missing right margin by @ralvarezing in #9102 - MudMask: Remove CatchAndLog usage by @ScarletKuro in #8970 - MudForm: Add `Spacing` property by @danielchalmers in #8880 - MudInputControl: Fix nested `
` inside `
` by @truongdatnhan in #8871 - MudSelect: Revert #8309 by @ScarletKuro in #8770 - MudSelect: Fix Un-SelectAll with Disabled MudSelectItems (#8420) by @JonasPerleryd in #8459 - MudCheckBox, MudRadio, MudSwitch: Fix shouldn't hover when ReadOnly and rename UncheckedColor by @henon in #8759 - Inputs: Add typography customization by @danielchalmers in #8754 - MudCheckBox: Add state CSS classes for easier testing by @henon in #8699 - BooleanInput: Fix form validation (#8690) by @igotinfected in #8693 - Input: Add `required` and `aria-required` attributes (#5437) by @igotinfected in #8691 - Input: Don't add margin-top when input has no Label by @ralvarezing in #8540 - MudBaseInput: match `input`s `id` with `label`'s `for` attribute (#6249, #6460) by @igotinfected in #8672 - MudBaseInput: Add nullable annotation. by @ScarletKuro in #8635 - MudBaseInput: Remove obsoleted KeyPress related APIs by @danielchalmers in #8476 - MudTextField: Remove last remaining reference to KeyPress by @danielchalmers in #8516

Global
- MudGlobal: Add `InputDefaults.ShrinkLabel` by @danielchalmers in #8935 - Globals: Add `DialogDefaults.DefaultFocus`, Consolidate others to their own static classes by @danielchalmers in #8831 - Global: Add transition duration and delay options by @danielchalmers in #8651
Grid
- MudGrid: Refactor and normalize spacing system by @danielchalmers in #8910 - MudGrid: Add nullable annotation. by @ScarletKuro in #8900
Icon
- MudIcon: Add font icon support with additional syntax by @ScarletKuro in #9026 - MudIcon: Add `role="img"` by @danielchalmers in #8915 - MudIcon: Fix icon color when disabled by @dennisrahmen in #8869
List
- MudListItem, MudMenu, MudMenuItem: Format razor markup by @danielchalmers in #9017 - MudListItem: SecondaryText Feature by @mckaragoz in #8921 - MudListItem: Replace Avatar and AvatarClass with AvatarContent by @henon in #8677 - MudList: Implement selection modes Single-, Toggle- and MultiSelection by @henon in #8775 - MudList: Generic MudList\ and other improvements by @henon in #8613 - MudListItem and MudMenuItem: Render as anchor with"Href" property (#1717) by @belucha in #8649
Menu
- MudListItem, MudMenu, MudMenuItem: Format razor markup by @danielchalmers in #9017 - MudMenu: Forward DropShadow property by @Yomodo in #8953 - MudMenu: Disable text selection in activator by @danielchalmers in #8719 - MudMenu: Improve Encapsulation, public API is now Async by @ScarletKuro in #8634 - MudMenu: Add nullable annotation. by @ScarletKuro in #8632 - Menu: Add `AriaLabel` property (#5783) by @igotinfected in #8710 - Menu: Improve touch support & Merge OnTouch,OnAction into OnClick by @danielchalmers in #8492 - MudListItem and MudMenuItem: Render as anchor with"Href" property (#1717) by @belucha in #8649
MudColor
- MudColor: Add uint support. by @BenMcLean in #9037 - MudColor: Make H, S, L readonly by @ScarletKuro in #8823 - MudColor: Add XML comments and make it serializable with STJ / NewtonsoftJson by @ScarletKuro in #8579
Navigation
- MudNavLink: Fix exception doesn't flow to ErrorBoundary. by @ScarletKuro in #9138 - MudNavGroup: Fix NavigationContext not updating on Expanded by @ScarletKuro in #9101 - MudNavGroup: Use ParameterState framework by @ScarletKuro in #8980
Overlay
- MudOverlay: Fix two way binding with AutoClose=true by @ScarletKuro in #8513
Pickers
- DateRangePicker: fix `Underline` parameter not working (#9124) by @igotinfected in #9140 - DateRangePicker: Fix DateFormat (#8737) by @dhess-dev in #8779 - DateRangePicker: Fix margin top missing when label is set by @ralvarezing in #8688 - DateRangePicker: Fix tests - update selected range to use future dates by @Anu6is in #8543 - MudPicker: Fix code style and naming issues by @ArieGato in #8517 - MudColorPicker, MudBaseDatePicker, MudTimePicker: Add localizable labels by @danielchalmers in #9022 - DatePicker: default OpenTo to Year when both FixDay and FixMonth are provided by @ZephyrZiggurat in #8932 - MudDatePicker: Fix GoToDate with Persian calendar by @ajahangard in #8909 - TimePicker: Prepare for touch support & ColorPicker: Prevent touch pan in static mode by @danielchalmers in #8800 - DatePickerTests: Fix CS1998, BL0005, nullable by @ScarletKuro in #8768 - Color Picker: Drag smoothly by @danielchalmers in #8576 - Pickers: change API to async to avoid async void and fire-and-forget by @ScarletKuro in #8506
Popover
- Popover: Allow panning by touch by @danielchalmers in #9208 - MudPopover: Add DropShadow property by @Yomodo in #8938
Progress
- Progress: Use ParameterState framework. by @ScarletKuro in #8433
Rating
- MudRating: Fix Color regression #5463 by @ScarletKuro in #9069 - MudRating: make HandleKeyDown async Task by @ScarletKuro in #8883 - MudRating: Use ParameterState framework by @ScarletKuro in #8877
Slider
- MudSlider: Rename Culture and ValueLabelFormat by @henon in #8954 - MudSlider: Add nullable value parameter by @ScarletKuro in #8881 - MudSlider: Add ValueLabelStringFormat, ValueLabelCultureInfo, ValueLabelContent by @ScarletKuro in #8760 - MudSlider: Use ParameterState and generic math by @ScarletKuro in #8745 - MudSlider: Fix tick count calculation for certain cases. Fixes #8713 by @digitaldirk in #8730
Snackbar
- Snackbar: Fix type class not taking into account per-snackbar options by @danielchalmers in #8423
Stack
- MudStack: Add parameter StretchItems to have certain children fill space by @BieleckiLtd in #8545
Table
- MudTable: Use InvokeAsync() in method InvokeServerLoadFunc by @Int32Overflow in #9261 - MudTable: add QuickColumns as illegal parameter by @ScarletKuro in #8930 - MudTable: Code cleanup. by @ScarletKuro in #8927 - MudTable: Add nullable annotation. by @ScarletKuro in #8926 - MudTable: Add ability to control which rows are editable by @biegehydra in #8873 - Table: Fix selection loss when applying filters and using Items property by @ralvarezing in #8538
Tabs
- Revert "MudTabs: add WrapHeaders (#9108)" by @henon in #9234 - MudTabs: add WrapHeaders (reverted) by @ZephyrZiggurat in #9108 - MudTabs: Add ActiveTabClass for better customization by @mueller-marcel in #8698 - MudTabs: Rename toolbar to tabbar in C# and CSS by @henon in #8569 - MudTabs: Fix overflow when using high border radius values by @Lyrapuff in #8561 - MudTabPanel: Allow icon color to be set from panel by @djflan in #9001
Text
- MudText: Revert removal of parameter Inline by @mckaragoz in #9065 - MudText: Return subtitle1 and subtitle2 to block elements (`p`) by @danielchalmers in #8899
Theme
- MudThemeProvider: Wrap JS in a try-catch by @ScarletKuro in #9042 - MudThemeProvider: Fix BL0007, avoid direct parameter writes, remove MudThemingProvider, and other improvements by @ScarletKuro in #8712 - Palette: Make all properties `virtual` & Improve docs by @danielchalmers in #8987 - Palette: Make abstract & Rename to PaletteLight in MudTheme and make type PaletteLight by @danielchalmers in #8453 - MudRipple: Fix ripple conditions by @meenzen in #8866 - MudRipple: Follow theme colors and improve visibility (#8072) by @meenzen in #8460 - Theme: Replace Palette to PaletteLight by @ScarletKuro in #8514 - Theming: Replace all "Grey" spellings with "Gray" by @danielchalmers in #8452
Toggle Group
- ToggleGroup: Replace `Dense` with `Size` & Use relative scaling by @danielchalmers in #8676 - ToggleGroup: Fix border rounding overflow on selected item by @danielchalmers in #8652 - ToggleGroup: Rename Outline to Outlined & Internal cleanup by @danielchalmers in #8616
Tooltip
- MudTooltip: Use ParameterState framework by @ScarletKuro in #8972 - MudTooltip: Add Disabled parameter by @Yomodo in #8876
TreeView
- MudTreeView: Change type of Items and ServerData to use TreeItemData\ by @henon in #9151 - MudTreeView: Add AutoExpand, ExpandAll() and CollapseAll() by @henon in #8762 - MudTreeView: Move checkbox icon customization from item to treeview by @henon in #8750 - MudTreeView: Add ripple effect (#8570) by @meenzen in #8669 - MudTreeView: Use ParameterState and rewrite selection logic from scratch + new features by @ScarletKuro in #8661 - TreeView: Update parent selection on item click in multi-selection mode by @henon in #8957 - TreeView: Add disabled style by @danielchalmers in #8707 - MudTreeviewItem: Fix checkbox in multi-selection mode by @henon in #8948 - TreeViewItem: Throw exception when ItemTemplate is misconfigured by @Taylan2020 in #8867
Virtualization
- MudVirtualize: Sync With Native Virtualize Parameters by @mckaragoz in #8700
New Components
- TimeSeriesChart: Add time label format option for time series chart by @jorisBarkema in #9049 - MudChart: Add TimeSeries line and area chart components by @radderz in #8973 - New Component: Add MudFlexBreak component for forcing flex items onto a new line by @danielchalmers in #8748
Misc Breaking Changes
- Components: Remove more obsolete members by @danielchalmers in #8532 - Components: Remove some obsoleted methods by @danielchalmers in #8477 - Components: Remove some obsolete Parameters and Tests by @danielchalmers in #8475 - Various components: Remove obsolete Link in favor of Href by @danielchalmers in #8471 - Various Components: Rename `DisableElevation` to `DropShadow` by @henon in #8592 - Various Components: Rename DisableUnderLine to Underline by @henon in #8591 - Various Components: Rename DisableGutters to just Gutters by @henon in #8580 - Various Components: Rename DisableRipple to Ripple by @henon in #8571 - Several Components: Remove obsolete API by @danielchalmers in #8564 - Icons: Remove Material.Obsolete, Update formatting by @danielchalmers in #8536 - EnumExtensions: Remove ToDescriptionString by @ScarletKuro in #8474 - DoubleExtensions: Fix typo, was DoubleExtentions by @ScarletKuro in #8473 - Remove: ResizeListenerService, ResizeService, BrowserWindowSizeProvider, BreakpointService by @ScarletKuro in #8467 - v7: Drop .NET6 as supported framework. by @danielchalmers in #8441
Misc New Features
- QuerySortExtensions: Make more trim safe, add more xml comments by @ScarletKuro in #9255 - feat: Escape Description attribute value by @xC0dex in #9238 - BrowserViewportService: Allow ReportRate of 0 to disable debounce by @ScarletKuro in #9145 - ParameterState: Make more meaningful names and add verify method by @ScarletKuro in #9141 - ParameterState: Solve inheritance problem (Solution 2) by @ScarletKuro in #9033 - ParameterState: Make it public available by @ScarletKuro in #8868 - ParameterState: Improve hot swappable IEqualityComparer system by @ScarletKuro in #8739 - ParameterState: Add scope registration, lock mechanism, ComponentBaseWithState by @ScarletKuro in #8683 - ParameterState: Func Comparer, implicit operator support by @ScarletKuro in #8629 - ParameterState: Add GetState extension by @ScarletKuro in #8450 - Utilities: Increase max spacing/gap to `20` from `16` by @danielchalmers in #8943
Misc Bug Fixes
- Components: Add missing base OnParametersSet by @ScarletKuro in #9226 - ComponentBase, Field, DateRangePicker: fix unreliable field id by @igotinfected in #9174 - ParameterState: Fix when subscribing only to EventCallback by @ScarletKuro in #8457 - mudScrollManager.js: Fix scrollToBottom ignoring scroll behavior by @ScarletKuro in #9134 - EventManager: Fix JsonSerializerOptions instances cannot be modified. by @ScarletKuro in #8979 - CssBuilder, StyleBuilder: Fix method with optional parameter is hidden by overload by @ScarletKuro in #8769 - MudRTLProvider: Use ParameterState by @ScarletKuro in #8765 - MudPageContentNavigation: Fix automatic highlighting of centered section by @epithet in #8711 - MudHidden: Use ParameterState framework. by @ScarletKuro in #8508
Misc
- SourceGenerator: Code improvements by @xC0dex in #9061 - TaskExtensions: Rename AndForget() to CatchAndLog() by @henon in #8950 - MudComponentBase: UserAttributes cannot be null by @ScarletKuro in #8680
Accessibility
- Docs: Fix regressions in v7 by @danielchalmers in #9206 - Docs: Improve accessibility for screen readers by @danielchalmers in #9075 - Docs: App bar tweaks to align with v6 & Accessibility by @danielchalmers in #8879 - MudFocusTrap: Don't hide focusable elements from accessibility tree by @danielchalmers in #9095 - Various components: Improve localization and ARIA by @danielchalmers in #9071 - MudText: Ability to control which HTML tag is used by @danielchalmers in #8916 - MudLink, MudPagination, MudSnackbarElement: Improve accessibility by @danielchalmers in #9064 - Progress: Improve accessibility by @danielchalmers in #9060 - MudCollapse, MudStack, MudToolbar: Improve accessibility by @danielchalmers in #9059 - Rating, ToggleGroup: Improve accessibility by @danielchalmers in #9058 - MudFocusTrap, MudFlexBreak, MudIcon, MudSpacer: Improve accessibility by @danielchalmers in #9007 - MudAvatar, MudAvatarGroup, MudBadge: Improve accessibility by @danielchalmers in #8986 - MudAlert, MudToggleIconButton, MudChip: Improve accessibility by @danielchalmers in #8966 - Accessibility: Specify website language by @danielchalmers in #8843 - Navigation: Improve accessibility (#4651, #4755, #4756) by @igotinfected in #8684 - MudNavMenu: Use `nav` instead of `div` for better accessibility by @igotinfected in #8674
Docs
- Docs: Update v7 announcement date by @danielchalmers in #9272 - Docs: Use same expand icon in nav menu by @danielchalmers in #9233 - Docs: Full fetched -\> Fully-fledged by @doxxx in #9198 - Docs: Improve MudTreeView server data example by splitting off server data by @henon in #9195 - Docs: Add another level of depth to the MudTreeView ServerData example by @henon in #9192 - Docs: Link to the related pull request on Release page by @Nickztar in #9110 - Docs: Remove alias Hiliter for Highlighter by @Olivervesth in #9088 - Docs: clean up file upload example by @igotinfected in #9036 - Docs: Announcement and roadmap for v7 GA by @danielchalmers in #9027 - Docs: MudDataGrid: Added XML Documentation for Public Members by @jperson2000 in #9000 - Docs: Update List Docs to replace Clickable with ReadOnly by @digitaldirk in #8981 - Docs: MudDrawer: Added XML Documentation for Public Members by @jperson2000 in #8955 - Docs: Refactor API Documentation Generator by @jperson2000 in #8903 - Docs: MudColorPicker: Added XML Documentation for Public Members by @jperson2000 in #8872 - Docs: Standard Language for XML Documentation for Previously Documented Classes by @jperson2000 in #8870 - Docs: MudButtonGroup/MudCard/MudCarousel/MudChart: Added XML Documentation for Public Members by @jperson2000 in #8810 - Docs: Fix MudDateRangePicker editable example by @birdalicious in #8806 - Docs: Fix missing binding in TimePicker example by @birdalicious in #8804 - Docs: Don't auto-focus search bar to allow keyboard navigation by @danielchalmers in #8786 - Docs: MudBadge/MudBreadcrumbs/MudButton: Added XML Documentation for Public Members by @jperson2000 in #8776 - Docs: Fix DataGridAggregationExample by @ScarletKuro in #8772 - Docs: Add note about `Accept` filter for `FileUpload` by @igotinfected in #8766 - Docs: Weight search toward closer matches for more accurate results by @danielchalmers in #8761 - Docs: Fix landing page not shrinking top section subtitle by @danielchalmers in #8757 - Docs: Revise v7 development announcement by @danielchalmers in #8756 - Docs: Use nameof in remaining cases of SectionContent.Code by @danielchalmers in #8747 - Docs: Misc refinements (April 16) by @danielchalmers in #8720 - Docs: Revert landing page search bar to icon by @danielchalmers in #8704 - Docs: Change DisableElevation to DropShadow by @ScarletKuro in #8703 - Docs: Cookie prompt style changes by @danielchalmers in #8696 - Docs: Make code copy button always visible by @danielchalmers in #8686 - Docs: Improve navigation UX & Several misc tweaks by @danielchalmers in #8682 - Docs: Update roadmap for v7 previews by @danielchalmers in #8681 - Docs: Fancify dialog styling example by @danielchalmers in #8668 - Docs: Add TitleContent to first Dialog example for completeness by @henon in #8663 - Docs: Merge nav menu links between all pages by @danielchalmers in #8655 - Docs: Improve appbar UX by @danielchalmers in #8643 - Docs: Fix Error When Creating Solution From MudBlazor Template in .NET 8 (#8546) by @jperson2000 in #8641 - Docs: Fix DarkLightMode button out of sync by @danielchalmers in #8638 - Docs: Fix contributor GitHub icon colors in dark mode by @Anu6is in #8633 - Docs: Add missing spaces in aliases and register Expander for Collapse by @danielchalmers in #8631 - Docs: Add danielchalmers and jperson2000 to Contributors by @danielchalmers in #8621 - Docs: Ignore case in search by @danielchalmers in #8614 - Docs: Cookie consent. by @ScarletKuro in #8602 - Docs: remove dead code in IconPage. by @ScarletKuro in #8600 - Docs: Fix link targets not working because of typo in property name by @danielchalmers in #8573 - Docs: Fix Regex by @ScarletKuro in #8544 - Docs: Style refinements & Always show search bar by @danielchalmers in #8515 - Docs: Replace notification with one for v7 & Don't hide read ones by @danielchalmers in #8505 - Docs: Fix binding regressions in examples by @danielchalmers in #8494 - Docs: Fix Color Picker example usage #8444 by @Malgorad in #8445 - Docs: Add Overline to Customization/Typography by @danielchalmers in #8432 - Docs: Replace obsolete usage, Fix typos, Use nameof in SectionContent.Code by @danielchalmers in #8422 - MudTable: Add XML Documentation for Public Members by @jperson2000 in #9189 - MudElement-MudFocusTrap: Add XML Documentation for Public Members by @jperson2000 in #9154 - MudDialog: Added XML Documentation for Public Members by @jperson2000 in #9096 - MudDialog: When content exceeds hight scroll only content, not title and action buttons by @epithet in #8785 - MudDivider/MudDrawer/MudDropZone: Add XML Documentation for Public Members by @jperson2000 in #9116 - MudPicker: Added XML Documentation for Public Members by @jperson2000 in #9048 - Installation: Update Add font and sytle references section by @Qwertyluk in #8969 - WasmHost / Examples.Data: Add nullable annotation. by @ScarletKuro in #8963 - Tests: Add support for specifying generic type parameters per component and remove class T by @ArieGato in #8962 - MudAutocomplete: Add XML Documentation for Public Members by @jperson2000 in #8701 - MudCheckBox/MudChip/MudChipSet/MudCollapse: Added XML Documentation for Public Members by @jperson2000 in #8829 - MudProgressLinear: fix XMLDoc of Value parameter by @markushaslinger in #8803 - MudAlert: Add XML Documentation for Public Members by @jperson2000 in #8618 - MudAvatar*: Added XML Documentation for Public Members by @jperson2000 in #8732 - MudBase*: Added XML Documentation for Public Members by @jperson2000 in #8721 - MudAppBar: Add XML Documentation for Public Members by @jperson2000 in #8640 - RenderQueueService: Changed From Singleton to Scoped by @jperson2000 in #8627 - ListColorExample: Fix color two way binding by @ScarletKuro in #8606 - CONTRIBUTING.md: Fix code sample by @ScarletKuro in #8601 - Optimization: Make PeriodicTableService a Singleton Service by @jperson2000 in #8582 - MudBaseSelectItem: Clarify OnClick behavior in respect to Href by @danielchalmers in #8425 - Column: Fix Sortable parameter documentation by @Qwertyluk in #8418
Build
- Build: Add LangVersion latest property by @xC0dex in #9191 - Build: Remove "Use primary constructor" suggestion by @mikes-gh in #9179 - Build: Update dotnet libs by @mikes-gh in #9178 - Build: Use latest github actions by @xC0dex in #9173 - Build: Remove AssemblyVersion and PackageVersion by @xC0dex in #9155 - Build: Update dotnet runtime libs by @mikes-gh in #8978 - Build: Fix invalid version string in assembly info for dotnet pack by @mikes-gh in #8968 - Build: Ignore version format check for assembly attribute by @mikes-gh in #8847 - Build: Use GitHub README for NuGet package by @danielchalmers in #8755 - Build: Ask for PRs to be labeled under only one category by @danielchalmers in #8708 - Build: Add Documentation PR type by @danielchalmers in #8689 - Build: Update wording for Issue and PR templates by @danielchalmers in #8595 - Build: Fix trim warnings when using dotnet format by @mikes-gh in #8518 - Build: Build and test all branches by @mikes-gh in #8469 - Build: Update copyright year, Exclude revision in informational version by @danielchalmers in #8431 - Snackbar: Make tests less flaky by @danielchalmers in #9074 - Snackbar: Improve reliability of `InterruptTransitions` test by @danielchalmers in #8929 - Tests: fix slider tests failing on certain cultures by @henon in #8949 - Tests: Clean up DOM references from recent PRs by @igotinfected in #8763 - Tests: Fix flaky ThrottleDispatcherTests by @ScarletKuro in #8612 - Tests: Fix ScrollToTopTests by @ScarletKuro in #8507 - Build(deps): Bump AspNetCore.SassCompiler from 1.72.0 to 1.74.1 in /src by @dependabot[bot] in #8609 - Build(deps): Bump ReportGenerator from 5.2.2 to 5.2.4 in /src by @dependabot[bot] in #8465 - Code Quality: Various refactorings by @danielchalmers in #8550 - Code Quality: dotnet format by @mikes-gh in #8523 - Code Quality: Remove Console message and unnecessary assignment. by @mikes-gh in #8519 - Refactor: Use regular expression source generator by @jperson2000 in #8533 - Refactor: Remove ICommand by @ScarletKuro in #8436 - Code Cleanup: Improved Performance of MudComponentBase, ResizeObserver, and EventListener by @jperson2000 in #8526
Contributors
Thank you to all the users who contributed to v7: @danielchalmers, @ScarletKuro, @henon, @jperson2000, @igotinfected, @mikes-gh, @xC0dex, @ralvarezing, @digitaldirk, @Anu6is, @ArieGato, @KapustinVadim1991, @Yomodo, @meenzen, @mckaragoz, @birdalicious, @epithet, @Qwertyluk, @dependabot[bot], @dennisrahmen, @ZephyrZiggurat, @belucha, @doxxx, @Nickztar, @Olivervesth, @Malgorad, @markushaslinger, @Int32Overflow, @biegehydra, @Jimmys20, @MihFig, @thedude61636, @kev-andrews, @peterthorpe81, @rmoroz20, @aditya119, @timlunev, @alexkleinwaechter, @mueller-marcel, @Lyrapuff, @djflan, @truongdatnhan, @JonasPerleryd, @Taylan2020, @Alerinos, @dhess-dev, @ajahangard, @LiZzeira, @jorisBarkema, @radderz, @BenMcLean, @neozhu, @BieleckiLtd.
New Contributors
Thank you to the new contributors who submitted their first PR in v7: @Alerinos, @BenMcLean, @JonasPerleryd, @KapustinVadim1991, @Lyrapuff, @Malgorad, @MihFig, @Nickztar, @Olivervesth, @aditya119, @alexkleinwaechter, @birdalicious, @dhess-dev, @djflan, @doxxx, @epithet, @jorisBarkema, @kev-andrews, @markushaslinger, @mueller-marcel, @neozhu, @radderz, @rmoroz20, @thedude61636, @timlunev, @truongdatnhan.
Full Changelog: v6.19.1...v7.0.0

Version v6.21.0
Released on Temmuz 06, 2024

What's Changed
Bug Fixes
MudForm: Support `For` expressions refering to non-nullable properties by @lieszkol in #9180
Other Changes
Code quality: Fix using order by @ScarletKuro in #9216
Build: Fix trim warnings when using dotnet format by @ScarletKuro in #9217
New Contributors
@lieszkol made their first contribution in #9180
Full Changelog: v6.20.0...v6.21.0

Version v7.0.0-rc.2
Released on Haziran 21, 2024

What's Changed
New Features
MudDialog: Add nullable annotation by @xC0dex in #9146
Add a Roslyn analyzer to detect parameter/attribute issues by @peterthorpe81 in #9031
MudBaseInput components: add helper text reference to `aria-describedby` attribute by @igotinfected in #9193
Bug Fixes
FileUpload: fix issue with file picker not opening in Safari with Blazor Server (#9148) by @igotinfected in #9157
FileUpload: fix uploading same file before and after clearing the input on chromium browsers (#9082) by @igotinfected in #9139
ComponentBase, Field, DateRangePicker: fix unreliable field id by @igotinfected in #9174
Popover: Allow panning by touch by @danielchalmers in #9208
MudFormComponent: call base OnParametersSet by @ScarletKuro in #9225
Components: Add missing base OnParametersSet by @ScarletKuro in #9226
MudDataGrid: Add correct class to the th element in HeaderCell by @Jimmys20 in #9203
Fix bug in parameter analyzer by @peterthorpe81 in #9229
New Contributors
@doxxx made their first contribution in #9198
Full Changelog: v7.0.0-rc.1...v7.0.0-rc.2

Version v7.0.0-rc.1
Released on Haziran 10, 2024

What's Changed
Breaking Changes
Buttons: Remove unnecessary `Title`, `AriaLabel` properties by @danielchalmers in #9098
MudTreeView: Change type of Items and ServerData to use TreeItemData by @henon in #9151
DataGrid: Change default values of TemplateColumn by @dennisrahmen in #9142
New Features
MudTabPanel: Allow icon color to be set from panel by @djflan in #9001
Various components: Improve localization and ARIA by @danielchalmers in #9071
MudTabs: add WrapHeaders by @ZephyrZiggurat in #9108
ParameterState: Make more meaningful names and add verify method by @ScarletKuro in #9141
Datagrid: Add NumberFormat and NumberCulture to AggregateDefinition (#9112) by @alexkleinwaechter in #9120
BrowserViewportService: Allow ReportRate of 0 to disable debounce by @ScarletKuro in #9145
Bug Fixes
MudDataGrid: Fix column resizing in RTL (#6965) by @thedude61636 in #9073
MudFocusTrap: Don't hide focusable elements from accessibility tree by @danielchalmers in #9095
MudDataGrid: Fixed serverData + virtualization bug (#5664) by @KapustinVadim1991 in #9086
MudNavGroup: Fix NavigationContext not updating on Expanded by @ScarletKuro in #9101
MudSwitch: Add missing right margin by @ralvarezing in #9102
mudScrollManager.js: Fix scrollToBottom ignoring scroll behavior by @ScarletKuro in #9134
MudNavLink: Fix exception doesn't flow to ErrorBoundary. by @ScarletKuro in #9138
DateRangePicker: fix `Underline` parameter not working (#9124) by @igotinfected in #9140
MudDataGrid: Refresh grouping after InvokeServerLoadFunc, ExpandAllGroups, CollapseAllGroups by @MihFig in #9150
New Contributors
@djflan made their first contribution in #9001
@thedude61636 made their first contribution in #9073
@Olivervesth made their first contribution in #9088
@Nickztar made their first contribution in #9110
@alexkleinwaechter made their first contribution in #9120
@MihFig made their first contribution in #9150
Full Changelog: v7.0.0-preview.4...v7.0.0-rc.1

Version v7.0.0-preview.4
Released on Mayıs 27, 2024

What's Changed
Breaking Changes
Autocomplete: Give async methods Async suffix by @danielchalmers in #8990
Replace mouse events with pointer events in applicable components by @danielchalmers in #8974
Remove MudSparkline component by @danielchalmers in #9008
MudMessageBox: Use ParameterState framework by @ScarletKuro in #9014
Fix DefaultConverter.DefaultCulture by @zerox981 in #9045
New Features
MudRating: Added optional parameters for Full and Empty Icon Color by @nejckikelj in #5463
MudAlert, MudToggleIconButton, MudChip: Improve accessibility by @danielchalmers in #8966
Palette: Make all properties `virtual` & Improve docs by @danielchalmers in #8987
MudAvatar, MudAvatarGroup, MudBadge: Improve accessibility by @danielchalmers in #8986
MudFocusTrap, MudFlexBreak, MudIcon, MudSpacer: Improve accessibility by @danielchalmers in #9007
MudChart: Add TimeSeries line and area chart components by @radderz in #8973
MudColorPicker, MudBaseDatePicker, MudTimePicker: Add localizable labels by @danielchalmers in #9022
MudColor: Add uint support. by @BenMcLean in #9037
MudIcon: Add font icon support with additional syntax by @ScarletKuro in #9026
DataGrid: ServerData with Virtualization by @KapustinVadim1991 in #9019
ParameterState: Solve inheritance problem (Solution 2) by @ScarletKuro in #9033
TimeSeriesChart: Add time label format option for time series chart by @jorisBarkema in #9049
MudText: Revert removal of parameter Inline by @mckaragoz in #9065
MudCollapse, MudStack, MudToolbar: Improve accessibility by @danielchalmers in #9059
Rating, ToggleGroup: Improve accessibility by @danielchalmers in #9058
MudLink, MudPagination, MudSnackbarElement: Improve accessibility by @danielchalmers in #9064
Bug Fixes
MudMask: Remove CatchAndLog usage by @ScarletKuro in #8970
EventManager: Fix JsonSerializerOptions instances cannot be modified. by @ScarletKuro in #8979
MudTooltip: Use ParameterState framework by @ScarletKuro in #8972
MudNavGroup: Use ParameterState framework by @ScarletKuro in #8980
DatePicker: default OpenTo to Year when both FixDay and FixMonth are provided by @ZephyrZiggurat in #8932
MudDataGrid: Removed Unused "Row" Class by @jperson2000 in #9002
MudMessageBox: Fix `visible` should be `Visible` by @danielchalmers in #9010
MudThemeProvider: Wrap JS in a try-catch by @ScarletKuro in #9042
Dialog: Revert #8964 (Fix possible deadlock) by @danielchalmers in #9030
MudRating: Fix Color regression #5463 by @ScarletKuro in #9069
Other Changes
Progress: Improve accessibility by @danielchalmers in #9060
New Contributors
@nejckikelj made their first contribution in #5463
@radderz made their first contribution in #8973
@BenMcLean made their first contribution in #9037
@KapustinVadim1991 made their first contribution in #9019
@zerox981 made their first contribution in #9045
@jorisBarkema made their first contribution in #9049
Full Changelog: v7.0.0-preview.3...v7.0.0-preview.4

Version v6.20.0
Released on Haziran 03, 2024

What's Changed
New Features
ParameterState: Add optional IEqualityComparer by @ScarletKuro in #8416
Progress: Use ParameterState framework. by @ScarletKuro in #8433
Bug Fixes
MudDialog: Fix some params not passed when inline by @danielchalmers in #8424
MudOverlay: Fix two way binding with AutoClose=true by @ScarletKuro in #8513
Full Changelog: v6.19.1...v6.20.0

Version v7.0.0-preview.3
Released on Mayıs 14, 2024

What's Changed
Breaking Changes
MudAutocomplete: `ItemDisabledTemplate` and `ItemSelectedTemplate` will now display even if `ItemTemplate` is not defined. by @digitaldirk in #8913
Standardise the use of `Hidden` and `HiddenChanged` by @BieleckiLtd in #8952
MudGrid: Refactor and normalize spacing system by @danielchalmers in #8910
New Features
MudGlobal: Add `InputDefaults.ShrinkLabel` by @danielchalmers in #8935
Utilities: Increase max spacing/gap to `20` from `16` by @danielchalmers in #8943
MudPopover: Add DropShadow property by @Yomodo in #8938
MudContainer: Add `Gutters` property by @danielchalmers in #8934
MudListItem: SecondaryText Feature by @mckaragoz in #8921
WasmHost / Examples.Data: Add nullable annotation. by @ScarletKuro in #8963
Bug Fixes
MudMenu: Forward DropShadow property by @Yomodo in #8953
MudTreeviewItem: Fix checkbox in multi-selection mode by @henon in #8948
MudDrawer: Improve breakpoint logic by @ScarletKuro in #8941
MudCard: Fix content not filling remaining space by @danielchalmers in #8933
MudDataGrid: Apply Footer/Header Style Funcs by @kev-andrews in #8853
TreeView: Update parent selection on item click in multi-selection mode by @henon in #8957
Dialog: Fix possible deadlock by @ScarletKuro in #8964
Other Changes
Tests: fix slider tests failing on certain cultures by @henon in #8949
New Contributors
@kev-andrews made their first contribution in #8853
Full Changelog: v7.0.0-preview.2...v7.0.0-preview.3

Version v7.0.0-preview.2
Released on Mayıs 09, 2024

What's Changed
Breaking Changes
MudDrawer: Fix initialization behaviour, use ParameterState, remove PreserveOpenState by @ScarletKuro in #8833
TreeViewItem: Throw exception when ItemTemplate is misconfigured by @Taylan2020 in #8867
Standardise the use of `Visible` by @BieleckiLtd in #8832
MudIcon: Fix icon color when disabled by @dennisrahmen in #8869
ToggleGroup: Replace `Dense` with `Size` & Use relative scaling by @danielchalmers in #8676
MudListItem and MudMenuItem: Render as anchor with"Href" property (#1717) by @belucha in #8649
MudSlider: Add nullable value parameter by @ScarletKuro in #8881
Standardise the use of `Selected` and `SelectedChanged` by @BieleckiLtd in #8886
Standardise the use of `ItemDisabled` by @BieleckiLtd in #8887
Standardise the use of `Active` by @BieleckiLtd in #8888
Standardise the use of `Open` and `OpenChanged` by @BieleckiLtd in #8891
Standardise the use of `Editable` by @BieleckiLtd in #8892
MudText: Ability to control which HTML tag is used by @danielchalmers in #8916
Dialog: Fix visibility of inline dialogs using Show method by @danielchalmers in #8925
New Features
ParameterState: Make it public available by @ScarletKuro in #8868
MudDataGrid: Add ICloneStrategy by @ScarletKuro in #8851
MudGrid: Add nullable annotation. by @ScarletKuro in #8900
MudTooltip: Add Disabled parameter by @Yomodo in #8876
MudTable: Add ability to control which rows are editable by @biegehydra in #8873
MudForm: Add `Spacing` property by @danielchalmers in #8880
MudTable: Add nullable annotation. by @ScarletKuro in #8926
Bug Fixes
MudAlert: Fix content alignment issue #8734 by @neozhu in #8735
MudRipple: Fix ripple conditions by @meenzen in #8866
MudRating: Use ParameterState framework by @ScarletKuro in #8877
MudRating: make HandleKeyDown async Task by @ScarletKuro in #8883
MudText: Return subtitle1 and subtitle2 to block elements (`p`) by @danielchalmers in #8899
Button: Revert sticky focus style by @danielchalmers in #8767
MudInputControl: Fix nested `
` inside `
` by @truongdatnhan in #8871

MudDatePicker: Fix GoToDate with Persian calendar by @ajahangard in #8909
MudIcon: Add `role="img"` by @danielchalmers in #8915
Autocomplete: Treat all ways of closing overlay the same by @danielchalmers in #8914
Other Changes
MudTable: add QuickColumns as illegal parameter by @ScarletKuro in #8930
New Contributors
@neozhu made their first contribution in #8735
@Taylan2020 made their first contribution in #8867
@creed-maxeta made their first contribution in #5343
@biegehydra made their first contribution in #8873
@truongdatnhan made their first contribution in #8871
Full Changelog: v7.0.0-preview.1...v7.0.0-preview.2

Version v7.0.0-preview.1
Released on Mayıs 01, 2024

What's Changed
Note: This Migration Guide #8447 should help you get your code ready for v7.0.0. If migration is too much work for you, you can keep using v6.x.x as long as the community maintains it with bug fixes and .net version upgrade PRs.
Breaking Changes
Refactor: Remove ICommand by @ScarletKuro in #8436
FileUpload: Fix top margin CSS by @danielchalmers in #8438
MudBreadcrumbs: Change from List to IReadOnlyList by @danielchalmers in #8439
MudExpansionPanel: Use ParameterState, remove obsolete API and change to async API by @ScarletKuro in #8446
v7: Drop .NET6 as supported framework. by @danielchalmers in #8441
MudDialogProvider: Add missing BackgroundClass (#8454) by @Alerinos in #8458
DoubleExtensions: Fix typo, was DoubleExtentions by @ScarletKuro in #8473
EnumExtensions: Remove ToDescriptionString by @ScarletKuro in #8474
Various components: Remove obsolete Link in favor of Href by @danielchalmers in #8471
Components: Remove some obsolete Parameters and Tests by @danielchalmers in #8475
Dialog: Rename ClassContent and ClassActions parameters by @ArieGato in #8481
MudAutocomplete: Remove SearchFuncWithCancel, Add CancellationToken to SearchFunc by @jperson2000 in #8490
MudTable: Add CancellationToken into ServerData Function for Cancelable Requests by @jperson2000 in #8407
MudBaseInput: Remove obsoleted KeyPress related APIs by @danielchalmers in #8476
Components: Remove some obsoleted methods by @danielchalmers in #8477
Theming: Replace all "Grey" spellings with "Gray" by @danielchalmers in #8452
MudText: Replace h6 with span for subtitle typos (#6059) by @tpmccrary in #6061
Palette: Make abstract & Rename to PaletteLight in MudTheme and make type PaletteLight by @danielchalmers in #8453
Update icons and remove obsolete API by @danielchalmers in #8421
MudChip and MudChipset: Support Generic by @mckaragoz in #4342
MudDataGrid: Fix grouping for bound and unbound scenarios using ParameterState by @peterthorpe81 in #8463
Menu: Improve touch support & Merge OnTouch,OnAction into OnClick by @danielchalmers in #8492
Input: Don't add margin-top when input has no Label by @ralvarezing in #8540
MudDataGrid: Respect indeterminate state in select-all checkboxes by @Qwertyluk in #8317
Several Components: Remove obsolete API by @danielchalmers in #8564
MudTabs: Rename toolbar to tabbar in C# and CSS by @henon in #8569
Various Components: Rename DisableRipple to Ripple by @henon in #8571
Chip: Don't apply hover or focus effect if not clickable and allow text selection by @danielchalmers in #8598
MudExpansionPanels: Rename `DisableBorders` to `Outlined` by @henon in #8593
Chip: Remove special palette variables by @danielchalmers in #8599
Various Components: Rename DisableGutters to just Gutters by @henon in #8580
MudList: Generic MudList and other improvements by @henon in #8613
ToggleGroup: Rename Outline to Outlined & Internal cleanup by @danielchalmers in #8616
Alert: Remove AlertTextPosition by @ScarletKuro in #8637
MudAvatar: Remove Obsolete Image Property by @Anu6is in #8648
Various Components: Rename `DisableElevation` to `DropShadow` by @henon in #8592
Various Components: Rename DisableUnderLine to Underline by @henon in #8591
MudDataGrid: Rename `DisableRowsPerPage` to `PageSizeSelector` by @henon in #8662
MudMenu: Improve Encapsulation, public API is now Async by @ScarletKuro in #8634
MudListItem: Replace Avatar and AvatarClass with AvatarContent by @henon in #8677
MudNavMenu: Use `nav` instead of `div` for better accessibility by @igotinfected in #8674
MudTreeView: Use ParameterState and rewrite selection logic from scratch + new features by @ScarletKuro in #8661
MudChipSet: use `SelectionMode` instead of `MultiSelect` and `Mandatory` by @henon in #8722
MudSlider: Use ParameterState and generic math by @ScarletKuro in #8745
Standardise the use of `Expanded`, `Expandable`, `IsExpanded` and `IsExpandable` by @BieleckiLtd in #8718
MudTreeView: Move checkbox icon customization from item to treeview by @henon in #8750
Standardise the use of `IsEnabled` and `Enabled` by @BieleckiLtd in #8764
MudCheckBox, MudRadio, MudSwitch: Fix shouldn't hover when ReadOnly and rename UncheckedColor by @henon in #8759
FileUpload: Rewrite using `ActivatorContent` pattern by @igotinfected in #8694
Snackbar: Remove MarkupString capability from Add(string message…) and add Add(MarkupString message…) (#8146) by @Conman-123 in #8156
ButtonGroup: Add `FullWidth`, Rename `VerticalAlign` to `Vertical` by @danielchalmers in #8798
Standardise the use of `Checked`, `CheckedChanged` and `Checkable` by @BieleckiLtd in #8825
Rename remaining Disable... properties by @henon in #8826
MudList: Implement selection modes Single-, Toggle- and MultiSelection by @henon in #8775
New Features
ParameterState: Add optional IEqualityComparer by @ScarletKuro in #8416
Progress: Use ParameterState framework. by @ScarletKuro in #8433
Snackbar: Only allow action invoke once, Add button classes, Add ForceClose by @danielchalmers in #8383
MudRipple: Follow theme colors and improve visibility (#8072) by @meenzen in #8460
MudToggleGroup and MudToggleItem: Add Disabled parameter (#8367) by @candritzky in #8377
MudStack: Add parameter StretchItems to have certain children fill space by @BieleckiLtd in #8545
ParameterState: Add GetState extension by @ScarletKuro in #8450
MudTable: Add ContainerStyle and ContainerClass parameters by @Etav99 in #6031
MudList: Add Nullable and use ParameterState by @ScarletKuro in #8297
Color Picker: Drag smoothly by @danielchalmers in #8576
MudMenu: Add nullable annotation. by @ScarletKuro in #8632
MudBaseInput: Add nullable annotation. by @ScarletKuro in #8635
MudDataGridPager: Add ShowNavigation and ShowPageNumber by @mckaragoz in #8363
ParameterState: Func Comparer, implicit operator support by @ScarletKuro in #8629
Buttons: Make Title a base property by @danielchalmers in #8630
Global: Add transition duration and delay options by @danielchalmers in #8651
MudCheckBox: Add state CSS classes for easier testing by @henon in #8699
ParameterState: Add scope registration, lock mechanism, ComponentBaseWithState by @ScarletKuro in #8683
MudTreeView: Add ripple effect (#8570) by @meenzen in #8669
TreeView: Add disabled style by @danielchalmers in #8707
MudVirtualize: Sync With Native Virtualize Parameters by @mckaragoz in #8700
MudBaseInput: match `input`s `id` with `label`'s `for` attribute (#6249, #6460) by @igotinfected in #8672
ParameterState: Improve hot swappable IEqualityComparer system by @ScarletKuro in #8739
MudTabs: Add ActiveTabClass for better customization by @mueller-marcel in #8698
Menu: Add `AriaLabel` property (#5783) by @igotinfected in #8710
Input: Add `required` and `aria-required` attributes (#5437) by @igotinfected in #8691
MudSlider: Add ValueLabelStringFormat, ValueLabelCultureInfo, ValueLabelContent by @ScarletKuro in #8760
MudTreeView: Add AutoExpand, ExpandAll() and CollapseAll() by @henon in #8762
New Component: Add MudFlexBreak component for forcing flex items onto a new line by @danielchalmers in #8748
MudAutocomplete: Open menu on focus, Rename `ToggleMenu` to `ToggleMenuAsync` by @danielchalmers in #8758
Inputs: Add typography customization by @danielchalmers in #8754
Globals: Add `DialogDefaults.DefaultFocus`, Consolidate others to their own static classes by @danielchalmers in #8831
Bug Fixes
MudDialog: Fix some params not passed when inline by @danielchalmers in #8424
ParameterState: Fix when subscribing only to EventCallback by @ScarletKuro in #8457
Tests: Fix ScrollToTopTests by @ScarletKuro in #8507
MudDateRangePicker: Add missing properties: PlaceholderStart/End and SeparatorIcon by @gaplin in #6431
DataGrid : Fix sorting when there is Header row (#7645) by @timlunev in #8504
MudHidden: Use ParameterState framework. by @ScarletKuro in #8508
Snackbar: Fix type class not taking into account per-snackbar options by @danielchalmers in #8423
Theme: Replace Palette to PaletteLight by @ScarletKuro in #8514
MudOverlay: Fix two way binding with AutoClose=true by @ScarletKuro in #8513
MudTextField: Remove last remaining reference to KeyPress by @danielchalmers in #8516
DateRangePicker: Fix tests - update selected range to use future dates by @Anu6is in #8543
Table: Fix selection loss when applying filters and using Items property by @ralvarezing in #8538
DataGrid: Fix row filter not working when columns are reordered. by @andrew2984 in #8400
FocusTrap: Fix outline focus by @LiZzeira in #7835
MudTabs: Fix overflow when using high border radius values by @Lyrapuff in #8561
MudSelect: Fix Un-SelectAll with Disabled MudSelectItems (#8420) by @JonasPerleryd in #8459
Code Cleanup: Improved Performance of MudComponentBase, ResizeObserver, and EventListener by @jperson2000 in #8526
MudColor: Add XML comments and make it serializable with STJ / NewtonsoftJson by @ScarletKuro in #8579
DataGrid: Fix empty footer row when using `SelectColumn` with `ShowInFooter=false` by @igotinfected in #7747
MudButton: fix focus style button when open dialog by @LiZzeira in #8575
MudAutocomplete: Fix missing margin-top when label is set by @ralvarezing in #8623
ToggleGroup: Fix border rounding overflow on selected item by @danielchalmers in #8652
DateRangePicker: Fix margin top missing when label is set by @ralvarezing in #8688
Various components: Prevent panning page by touch by @danielchalmers in #8394
MudSlider: Center track tick labels by @DennisOstertag in #7302
BooleanInput: Fix form validation (#8690) by @igotinfected in #8693
Button: Don't apply :focus effect on mobile so it's not sticky by @danielchalmers in #8709
MudMenu: Disable text selection in activator by @danielchalmers in #8719
MudSlider: Fix tick count calculation for certain cases. Fixes #8713 by @digitaldirk in #8730
Navigation: Improve accessibility (#4651, #4755, #4756) by @igotinfected in #8684
MudDialog: Fix content not stretched in fullscreen mode by @danielchalmers in #8743
MudSelect: Revert #8309 by @ScarletKuro in #8770
CssBuilder, StyleBuilder: Fix method with optional parameter is hidden by overload by @ScarletKuro in #8769
MudRTLProvider: Use ParameterState by @ScarletKuro in #8765
MudPageContentNavigation: Fix automatic highlighting of centered section by @epithet in #8711
Better Illegal Razor Parameter Runtime Detection for v7 by @henon in #8777
DateRangePicker: Fix DateFormat (#8737) by @dhess-dev in #8779
MudThemeProvider: Fix BL0007, avoid direct parameter writes, remove MudThemingProvider, and other improvements by @ScarletKuro in #8712
DataGrid: Remove 'Value not null' criteria for FilterDefinition by @aditya119 in #8706
Autocomplete: Improve menu behavior by @danielchalmers in #8787
Other Changes
Build: Add Documentation PR type by @danielchalmers in #8689
Build: Ask for PRs to be labeled under only one category by @danielchalmers in #8708
Tests: Clean up DOM references from recent PRs by @igotinfected in #8763
Illegal Razor Parameter Runtime Detection for v7 by @henon in #8771
New Contributors
@Alerinos made their first contribution in #8458
@jperson2000 made their first contribution in #8490
@tpmccrary made their first contribution in #6061
@timlunev made their first contribution in #8504
@candritzky made their first contribution in #8377
@andrew2984 made their first contribution in #8400
@LiZzeira made their first contribution in #7835
@Lyrapuff made their first contribution in #8561
@JonasPerleryd made their first contribution in #8459
@Etav99 made their first contribution in #6031
@Malgorad made their first contribution in #8445
@mueller-marcel made their first contribution in #8698
@epithet made their first contribution in #8711
@dhess-dev made their first contribution in #8779
@Conman-123 made their first contribution in #8156
@rafalmaciag made their first contribution in #8799
@markushaslinger made their first contribution in #8803
@birdalicious made their first contribution in #8806
@aditya119 made their first contribution in #8706
Full Changelog: v6.19.1...v7.0.0-preview.1

Version v6.19.1
Released on Mart 22, 2024

What's Changed
Bug Fixes
Docs: Add fuzzy search & Make spaces in names consistent by @danielchalmers in #8276
Full Changelog: v6.19.0...v6.19.1

Version v6.19.0
Released on Mart 22, 2024

What's Changed
New Features
MudToggleIconButton: Add DisableElevation and ClickPropagation properties by @danielchalmers in #8404
Autocomplete: Add class templates and FoundItemsCount (and improve docs mobile search dialog) by @danielchalmers in #8362
Bug Fixes
MudRadio: Fix Stack overflow caused by recurrence. by @isakgamnes in #8412
MudTimePicker: Double pad hours like minutes already are by @danielchalmers in #8406
Use ActionDefault as the base for ActionDefaultHover by @danielchalmers in #8405
New Contributors
@isakgamnes made their first contribution in #8412
Full Changelog: v6.18.0...v6.18.1

Version v6.18.0
Released on Mart 20, 2024

What's Changed
Breaking Changes
DataGrid: Throw exception when ServerData and Items are supplied (#8020) by @vernou in #8026
New Features
Introduce ParameterState framework to facilitate parameter change logic and avoid BL0007 by @ScarletKuro in #8258
MudStack: Add Wrap Property by @skyslide22 in #8290
Localization: Implement ILocalizationInterceptor by @Meduris in #7389
~~BrowserViewportService~~ & PopoverService: Decrease PoolSize and set PoolInitialFill by @ScarletKuro in #8305
Localization: Extend AbstractLocalizationInterceptor to be less specific. by @ScarletKuro in #8306
DataGridRowValidator : Mark the Model property as virtual by @iDerrien in #7771
Dialog: Add ClassTitle property to enable custom styling of the Title by @ArieGato in #8332
MudInput: Add class when AutoGrow is enabled by @danielchalmers in #8234
Snackbar: Keep visible while touched or hovered by @danielchalmers in #8334
ParameterState: Call the handler once when parameters reference the same handler. by @ScarletKuro in #8384
MudFileUpload: Add nullable annotation by @ScarletKuro in #8392
ParameterState: Add ParameterChangedEventArgs to get previous and current value by @ScarletKuro in #8386
MudTable: Provide contextual item data when using EditButtonContent by @stho01 in #6129
MudDataGrid: Added RemoveColumn method by @Jimmys20 in #8288
Input: Make AutoGrow responsive to parameters & Fix empty line after scrollbar is hidden by @danielchalmers in #8385
Bug Fixes
DatePicker: Fix PerisanCalender change year issue (#3740) by @ajahangard in #7484
Fix BlockMask: Adjust Regex generation to allow a strict prefix by @Anu6is in #8319
MudNumericField: Add missing ShrinkLabel by @Etzix in #8320
MudLineChart: All zero series data no longer throws exception by @Anu6is in #8327
MudNumericField: Fix decrementing from null value by @Anu6is in #8326
MudBarChart: Single x-axis value no longer throws exception by @Anu6is in #8333
Various components: Avoid re-renderings from interaction events by @danielchalmers in #8281
MudDateRangePicker: Ignore timestamp when setting day classes by @Anu6is in #8342
Fix Mistaken CSS Reordering by @mckaragoz in #8345
TreeView: Fix the ParameterView instance has expired. by @ScarletKuro in #8361
PopoverService: Attemp to fix SemaphoreFullException by @ScarletKuro in #8325
MudDialog: Change default value of DefaultFocus to "Element" to shift the focus into the dialog. by @Etzix in #8366
MudTextField: Fix scrollbars appearing with AutoGrow on scaled displays due to rounding error by @danielchalmers in #8329
Buttons: Fix exception doesn't flow to ErrorBoundary. by @ScarletKuro in #8369
BrowserViewportService: Revert #8236 AsyncKeyedLock optimization by @ScarletKuro in #8372
MudLink: Fix exception doesn't flow to ErrorBoundary. by @ScarletKuro in #8375
Various components: Hide blue tap highlight that occurs on mobile by @danielchalmers in #8336
Fix sticky hover effects on mobile by @danielchalmers in #8256
DatePicker: Show first selected date when DatePicker is nested by @Anu6is in #8382
Other Changes
AsyncKeyedLock: Rename folder and add link to the original source code. by @ScarletKuro in #8315
New Contributors
@skyslide22 made their first contribution in #8290
@iDerrien made their first contribution in #7771
@ajahangard made their first contribution in #7484
@Anu6is made their first contribution in #8319
@ArieGato made their first contribution in #8332
@stho01 made their first contribution in #6129
@Jimmys20 made their first contribution in #8288
Full Changelog: v6.17.0...v6.18.0

Version v6.17.0
Released on Mart 04, 2024

What's Changed
New Features
DialogService: Constrain Dialogs to IComponent instead of ComponentBase by @ScarletKuro in #8232
PopoverService: Improve performance by @ScarletKuro in #8210
Bug Fixes
DataGrid: filter for nullable DateTime is missing in DataGridFilterMode.ColumnFilterRow mode by @ScarletKuro in #8201
MudDataGrid: Do not add duplicate filters by @0xced in #7594
MudButton: Fix extra re-render by @ScarletKuro in #8203
MudColor: Fix ColorRgbDarken and ColorRgbLighten by @50c in #8206
MudTable: Retain selection when using ServerData by @ralvarezing in #7914
DataGrid: Fix editing of nullable values such as int? by @snakex64 in #8231
MudTextField: Remove phantom scrollbars when auto grow is enabled and height is uncapped by @danielchalmers in #8235
BrowserViewportService: Improve performance by @ScarletKuro in #8236
IScrollSpy: Fix DotNetObjectReference instance was already disposed by @ScarletKuro in #8239
Revert "MudButton: Fix hover effect remaining after touch" by @henon in #8247
MudTable: Fix excessive SelectedItemsChanged callback by @Qwertyluk in #8266
MudTabs: Vertical tabs headers now stretch to fill available horizontal space by @pingu2k4 in #8259
New Contributors
@0xced made their first contribution in #7594
@50c made their first contribution in #8206
@ralvarezing made their first contribution in #7914
@Qwertyluk made their first contribution in #8212
@davidxuang made their first contribution in #8242
@pingu2k4 made their first contribution in #8259
Full Changelog: v6.16.0...v6.17.0

Version v6.16.0
Released on Şubat 19, 2024

What's Changed
New Features
Add X (formerly Twitter) logo to Brands.cs by @marknoble in #8135
MudBaseInput: Add "ShrinkLabel" parameter that prevents label from moving by @Etzix in #8131
DataGrid: Support for sub properties editing by @snakex64 in #8084
DataGrid: Add IsOpened property to CellContext (#7432) by @MarDipp in #7765
MessageBox: Add "mud-message-box" class by @danielchalmers in #8189
Bug Fixes
MudDynamicTabs: Fix disappearing slider with key attribute (#2816) by @gaplin in #8110
MudDrawer: Opening mini drawer pushes MudMainContent to the right (#7775) by @Eagle3386 in #8137
MudChart: Fix series coloring issue with more than 20 custom colors (#8099) by @Alanocorleo in #8117
MudTimePicker: Fix AM|PM (#8032) by @vernou in #8150
X Icon Fix by @mckaragoz in #8158
MudDataGrid: Make grouping two-way bindable (#8159) by @Gopichandar in #8160
Skeleton: Allow changes after initialization by @VStefanWeber in #8178
DataGrid: Fix FilterDefinition.Title not updating when selected field is changed by @Saman-00 in #7821
MudButton: Fix hover effect remaining after touch by @danielchalmers in #8188
MudTextField: Preserve scroll position while typing with AutoGrow and update when window is resized by @danielchalmers in #8193
MudTextField: Fix double validation on blur (#7034) by @vernou in #8121
MudMenu: Fix when PositionAtCursor is true and touch is used. by @ScarletKuro in #8194
New Contributors
@Alanocorleo made their first contribution in #8117
@marknoble made their first contribution in #8135
@Etzix made their first contribution in #8131
@VStefanWeber made their first contribution in #8178
@Saman-00 made their first contribution in #7821
@MarDipp made their first contribution in #7765
Full Changelog: v6.15.0...v6.16.0

Version v6.15.0
Released on Ocak 30, 2024

What's Changed
New Features
MudTreeView: Fix selection issues by @jacob7395 in #7810
MudTable: make OnRowClicked async/awaitable by @FeuFeve in #8059
MudThemingProvider: MudThemeProvider without MudPopoverProvider by @mikes-gh in #8102
Bug Fixes
MudTable: Optimize event handling for OnRowMouseEnter/Leave by @haas-daniel in #8081
Full Changelog: v6.14.0...v6.15.0

Version v6.14.0
Released on Ocak 22, 2024

Note
This is a rerelease of 6.13.0 removing
MudPopoverProvider: Don't static render #8057
due to a breaking change for MAUI users
What's Changed
Breaking Changes
MudToggleGroup: Rename SelectedValues and warn if misconfigured by @henon in #7994
DataGrid: Throw exception when ServerData and QuickFilter are supplied (#7998) by @vernou in #8010
New Features
DataGrid: Make RowsPerPage two-way bindable by @BossManta in #8033
MudTable: add OnRowMouseEnter and OnRowMouseLeave events by @FeuFeve in #8014
Bug Fixes
MudTextField: Fix double triggering of validation on blur (#7034) by @vernou in #7996
MudTable: fix Bordered in RTL mode by @henon in #8008
BrowserViewportService: Fix when the circuit has disconnected in BSS by @ScarletKuro in #8071
New Contributors
@vernou made their first contribution in #7996
@LuisThe0ne made their first contribution in #8013
@BossManta made their first contribution in #8033
@FeuFeve made their first contribution in #8014
Full Changelog: v6.12.0...v6.14.0

Version v6.12.0
Released on Ocak 02, 2024

What's Changed
Addition of net8.0 target
There is now a `net8.0` native version of the library in addition to the existing `net7.0` and `net6.0` versions in the package. This is automatically selected depending on your applications target framework.
Note you can still use older versions of the package in `net8.0` without issue.
New Features
MudAvatarGroup: Add MaxAvatarsTemplate by @Kaerlon in #7896
New Component: MudToggleGroup by @mckaragoz in #7309
New Component: MudToggleGroup by @mckaragoz in #7948
Bug Fixes
mudHelpers.js: Add missing closing bracket by @REDECODE in #7940
MudDataGrid: Fix ItemDrop Event Trigger Issue with Disabled ColumnsPanelReordering by @peterthorpe81 in #7934
MudTabs: Rightmost tab is not fully scrolled into view when active by @TDroogers in #7876
MudDatePicker: Setting date null doesn't clear invalid text by @jacob7395 in #7866
ToggleGroup: Improvements by @mckaragoz in #7975
New Contributors
@Kaerlon made their first contribution in #7896
@REDECODE made their first contribution in #7940
@jacob7395 made their first contribution in #7866
Released on Aralık 18, 2023

What's Changed
New Features
MudDataGrid: Implement more complete columns panel by @peterthorpe81 in #7632
Bug Fixes
Build: Move global.json so it used in all repo ops by @mikes-gh in #7802
Fix html hidden attribute is being overridden by @ilovepilav in #7806
MudMenuItem: Don't "click" when scrolling menu on touch devices (#7262) by @ilovepilav in #7809
ObserverManager: Fix collection moddified. by @ScarletKuro in #7843
MudChipSet: Fix unable to select same item after reset (#7841) by @ilovepilav in #7846
MudTextField: Fix initial height if AutoGrow is enabled (#7839) by @AntMaster7 in #7849
Build: Fix XML comment warnings by @mikes-gh in #7890
Other Changes
Inputs: Consistent Naming of the Value Parameter by @mckaragoz in #7892
New component: StackedBarChart by @Nooppi69 in #6267
LineChart: Allow to hide graph series by @Gatos90 in #7869
LineChart: Add a little space between checkbox and text in legend by @henon in #7924
LineChart: Improve Documentation and Examples by @henon in #7926
New Contributors
@ilovepilav made their first contribution in #7806
@Gatos90 made their first contribution in #7872
@danielchalmers made their first contribution in #7848
@Nooppi69 made their first contribution in #6267
@peterthorpe81 made their first contribution in #7632
Full Changelog: v6.11.1...v

Version v6.11.1
Released on Kasım 21, 2023

What's Changed
New Features
MudRipple: Show ripple effect based on click position (#157) by @meenzen in #7637
TestViewer: Better search and other minor tweaks by @chausner in #7353
MudTextInput: Add AutoGrow feature (#7631) by @AntMaster7 in #7644
MudDataGrid: Added Context menu row click by @fondraco in #7411
MudDropZone: Add ItemClassSelector parameter. by @RPalejiya in #7599
MudMenuItem: Add OnAction for either click or touch but invoked only once by @GRMagic in #7651
MudLineChart: remove Integrate() call because the return not used by @GeeSuth in #7691
MudForm: Add ResetTouched() to reset IsTouched w/o resetting the form (#7352) by @DennisOstertag in #7513
MudBaseButton: Add documentation for the ClickPropagation property. by @Amonteverde04 in #7733
MudAutoComplete: Add ListItemClass by @BieleckiLtd in #7737
MudFileUpload: Improve validation in MudForm by @igotinfected in #7720
Bug Fixes
MudDataGrid: Adjust simple filter width for consistency (#7538) by @MohamedYassin-J in #7602
MudForm: Catch all OnTimerComplete errors and log to console. by @Mr-Technician in #7626
ButtonGroupExamples: Fix alignment in split button section by @MohamedYassin-J in #7604
ScrollToTopExamples: Correct Setup section button positioning by @MohamedYassin-J in #7617
MudFormComponent: Update ValidationMessages on EditContext state change by @AntMaster7 in #7657
MudTablePager: Fix Vertical Alignment of Select text (#7501, #7507) by @phamilton4321 in #7670
MudSelect: Allow setting Class on various internal elements by @mckaragoz in #7596
MudDataGrid: Remove redundant column menu (#7566) by @MohamedYassin-J in #7627
MudDataGrid: Fix HierarchyColumn button not clickable on glyph by @nicoarm93 in #7706
BrowserViewportService: Ignore subscription when disposed by @ScarletKuro in #7780
Other Changes
Docs: Fixed small typo in FormPage.razor by @erinnmclaughlin in #7705
New Contributors
@AntMaster7 made their first contribution in #7657
@digitaldirk made their first contribution in #7663
@RPalejiya made their first contribution in #7599
@erinnmclaughlin made their first contribution in #7705
@Amonteverde04 made their first contribution in #7733
Full Changelog: v6.11.0...v6.11.1

Version v6.11.0
Released on Ekim 08, 2023

What's Changed
Breaking Changes
MudAutocomplete: Fix doubling of the AfterItemsTemplate by @fondraco in #7613
New Features
MudDataGrid: Add ability to mark properties not required during editing (#7325) by @Eddie-Hartman in #7412
Bug Fixes
MudCarousel: fix incorrect transition description by @mvdgun in #7509
MudTablePager: Fix Vertical Alignment of Select text (#7501) by @phamilton4321 in #7507
MudTimePicker: Fix hour not updating when changed more than once (#7483) by @igotinfected in #7517
MudTable: Fix sticky footer in scss (#2602) by @stevencjy in #5608
MudMask: Handle multiline input case by @youssefbennour in #7548
MudDatePicker: Retain specific DateTime.Kind when setting undefined kind by @GRMagic in #7564
DataGridExamples: Fix item removal when list is empty by @MohamedYassin-J in #7571
MudFormComponent: Fix bad test causing invalid operation exception by @igotinfected in #7579
MudFileUpload: Fix validation in MudForm (#7577) by @igotinfected in #7578
MudDataDrid: Validation errors now prevent changes being committed (#6748) by @benm-eras in #6760
MudTable: Fix misaligned loading bar by @BieleckiLtd in #7576
New Contributors
@mvdgun made their first contribution in #7509
@phamilton4321 made their first contribution in #7507
@stevencjy made their first contribution in #5608
@youssefbennour made their first contribution in #7548
@Eddie-Hartman made their first contribution in #7412
@GRMagic made their first contribution in #7564
@MohamedYassin-J made their first contribution in #7571
@benm-eras made their first contribution in #6760
Full Changelog: v6.10.0...v6.11.0

Version v6.10.0
Released on Eylül 14, 2023

What's Changed
New Features
Buttons: Add ClickPropagation parameter by @mckaragoz in #7343
Support negative y axes in line and bar charts by @chausner in #7401
MudDropZone: Fix for nested drop zones by stopping event propagation by @prkhsn in #7424
Buttons: Add nullable annotation. by @ScarletKuro in #7427
MudChip, MudAvatar: Fix visual bug by @fondraco in #7406
MudPicker: Add parameter ImmediateText to update Text while typing (#7390) by @dennml in #7391
MudMenu: Add IsOpenChanged event by @ronimizy in #7463
Popover: add Legacy mode (uses old MudPopoverService) by @ScarletKuro in #7497
MudCard: Add nullable annotation. by @ScarletKuro in #7428
Bug Fixes
MudTextField: Fix InputMode not propagated (#4790) by @gaplin in #7380
MudTable: Fix the shifting of striped rows when loading by @BieleckiLtd in #7384
MudAutocomplete: Fix double scrollbars with Before/AfterItemsTemplate by @fondraco in #7387
MudTimeline: Make horizontal layout responsive by @BEEG3R in #7402
DebouncedTextFieldFormatChangeRerenderTest: fix culture issue on non-english locales by @Meduris in #7400
MudTable: Remove double border when a footer is used by @Geccoka in #7393
Snackbar: Improve trimming exclusion by @ScarletKuro in #7429
MudDataGrid: Fix for Guid column filtering (#7381) by @jonathanpotts in #7383
PopoverService: do not call JS during OnInitializedAsync by @ScarletKuro in #7441
MudFileUpload: Update summary for MaximumFileCount to better convey its intention (#7227) by @vorsprungdev in #7447
MudDataGrid: #7440 Fix new groups doesnt respect GroupExpanded property. by @pariv in #7444
MudTabs: Fix ActivatePanel ArgumentOutOfRange- and NullReferenceException by @Yomodo in #7459
MudTable/MudDataGrid: Fix row background color for mobile view. by @KBM-Kevin-Kessler in #7306
MudScrollManager: fix scroll lock for touch devices by @ric-ros in #7472
New Contributors
@BEEG3R made their first contribution in #7402
@prkhsn made their first contribution in #7424
@jonathanpotts made their first contribution in #7383
@vorsprungdev made their first contribution in #7447
@pariv made their first contribution in #7444
@ronimizy made their first contribution in #7463
@ric-ros made their first contribution in #7472
Full Changelog: v6.9.0...v6.10.0

Version v6.9.0
Released on Ağustos 18, 2023

What's Changed
New Features
MudDataGrid: New parameter "SelectOnRowClick" by @DrastamatSargsyan in #6547
MudMenu: Add IsOpen property (#7266) by @martinstoeckli in #7290
MudFocusTrap: Add nullable annotation. by @ScarletKuro in #7331
Optimization: Replace .Count() > 0 with .Any() by @bdebaere in #6809
MudInputControl: Add nullable annotation. by @ScarletKuro in #7340
TimePicker: Added MinuteSelectionStep Parameter by @Zingabopp in #7174
MudRadio: Add nullable annotation. by @ScarletKuro in #7366
Bug Fixes
MudColor: Change ToString default mode to RGBA (#7275) by @ITCBB in #7303
MudDataGrid: Fix CurrentPage not working (#7267) by @alasdair-cooper in #7308
Prerendering: Check if JSRuntime is available by @ScarletKuro in #7333
Refactor charts, BarChart and LineChart layout fixes by @chausner in #7345
MudDatePicker: Fix CSP JavaScript issues by @Schaeri in #7295
Other Changes
DataGridAdvancedExample.razor: Fix typo "gobally" by @hybrid2102 in #7300
New Contributors
@AlessandroMartinelli made their first contribution in #7294
@hybrid2102 made their first contribution in #7300
@ITCBB made their first contribution in #7303
@alasdair-cooper made their first contribution in #7308
@DrastamatSargsyan made their first contribution in #6547
@martinstoeckli made their first contribution in #7290
@MagicBOTAlex made their first contribution in #7327
@Zingabopp made their first contribution in #7174
Full Changelog: v6.8.0...v6.9.0

Version v6.8.0
Released on Ağustos 02, 2023

What's Changed
New Features
MudDrawer: Add nullable annotation. by @ScarletKuro in #7186
MudTable/MudDataGrid: Add ItemSize parameter for virtualization by @ArcadeMode in #7190
Bug Fixes
Fix DataGrid Select-All behaviour when filtered by @rathga in #7167
TimePicker: Handle conversion errors by @Cerberus4444 in #6947
MudDebouncedInput: Fix swallowed user input on component re-render (#7193) by @igotinfected in #7194
MudRangeInput: fix clearable button is misaligned by @tavanuka in #7200
MudDataGrid: Fix when nested property has same name by @ScarletKuro in #7198
Docs: Fix reference type issue on `DropZone` reorder save example (#7191) by @igotinfected in #7209
Date/Time Pickers: Add DisableUnderline param like in MudTextField by @DennisOstertag in #7226
MudDataGrid: SelectColumn not rendered in DataGrid with enabled edit mode by @o-khytrov in #7207
MudMask: Fix Clearable with non-PatternMasks (#7238) by @igotinfected in #7244
Tooltip: Fix arrow placement when using ChildContent by @Mr-Technician in #7271
New Contributors
@rathga made their first contribution in #7167
@Cerberus4444 made their first contribution in #6947
@ArcadeMode made their first contribution in #7190
@tavanuka made their first contribution in #7200
@DennisOstertag made their first contribution in #7226
@o-khytrov made their first contribution in #7207
Full Changelog: v6.7.0...v6.8.0

Version v6.7.0
Released on Temmuz 10, 2023

What's Changed
New Features
MudPagination: Add nullable annotation. by @ScarletKuro in #7124
MudPageContentNavigation: Add nullable annotation. by @ScarletKuro in #7125
MudBreadcrumbs: Add nullable annotation. by @ScarletKuro in #7126
DateRangePicker: Add Parameter Clearable by @mckaragoz in #6430
Make FilterDefinition compatible with EF Core by @ChasakisD in #7109
Themes: nullable & XML comments by @ScarletKuro in #7089
MudBaseButton: Add nullable annotation. by @ScarletKuro in #7136
MudMessageBox: Add nullable annotation. by @ScarletKuro in #7137
BrowserViewportService: Rework of IBreakpointService & IResizeService by @ScarletKuro in #7098
MudCarousel: Add nullable annotation. by @ScarletKuro in #7138
ResizeService & BreakpointService: Obsolete by @ScarletKuro in #7139
Add SourceLink support by @ScarletKuro in #7143
MudLayout: Add nullable annotation. by @ScarletKuro in #7148
MudRender: Add nullable annotation. by @ScarletKuro in #7149
Extensions: Add nullable annotation. by @ScarletKuro in #7151
MudSparkLine: Add nullable annotation. by @ScarletKuro in #7152
Nuget package: Use PackageLicenseExpression instead of PackageLicenseFile by @korteksz in #7168
MudLink: Add nullable annotation. by @ScarletKuro in #7175
MudNavMenu: Add nullable annotation. by @ScarletKuro in #7176
Bug Fixes
MudDatePicker: Fix ArgumentOutOfRangeException (#7120) by @gaplin in #7127
Popover: Fix "Cannot read properties of null"(JS) by @ScarletKuro in #7131
mudResizeListener.js: Fix cancelListener by @ScarletKuro in #7140
MudForm/MudBaseInput: Fix swallowed user input on `FieldChanged` re-render (#7158) by @igotinfected in #7159
MudDataGrid Fix: Added the EditDialogOptions to inline MudDialog for the DataGridEditMode.Form by @ingkor in #7162
MudDataGrid: Fix SingleSelection by @wesProg23 in #7021
MudFileUpload: Add AppendMultipleFiles property by @joe-gregory in #7027
MudTabs: Fix flickering with initial ActivePanelIndex != 1 by @Geccoka in #7058
New Contributors
@ChasakisD made their first contribution in #7109
@alfonso-martin-tapia made their first contribution in #7141
@korteksz made their first contribution in #7168
@Geccoka made their first contribution in #7058
Full Changelog: v6.6.0...v6.7.0

Version v6.6.0
Released on Haziran 29, 2023

What's Changed
New Features
MudCheckBox: Add nullable annotation. by @ScarletKuro in #7082
MudElement: Add nullable annotation. by @ScarletKuro in #7083
MudPaper: Add nullable annotation. by @ScarletKuro in #7084
MudHidden: Add nullable annotation. by @ScarletKuro in #7087
MudSwitch: Add nullable annotation. by @ScarletKuro in #7088
EventUtil: nullable & XML comments by @ScarletKuro in #7090
MudTimeline: Add nullable annotation by @meenzen in #7096
MudTooltip: Add nullable annotation by @meenzen in #7094
MudToolBar: Add nullable annotation by @meenzen in #7095
MudThemeProvider: Add nullable annotation by @meenzen in #7097
MudHighlighter: Add Markup parameter to render Text using HTML Markup by @Yomodo in #7092
MudSkeleton: Add nullable annotation. by @ScarletKuro in #7113
MudScrollToTop: Add nullable annotation. by @ScarletKuro in #7114
Bug Fixes
DataGridServerMultiSelectionTest: use PropertyColumn instead by @ScarletKuro in #7079
ResizeListenerService & BreakpointService: Allow user defined breakpoints by @ScarletKuro in #7074
Localization: fix net6.0 dependencies by @Meduris in #7104
PopoverService: Clear all observers on DisposeAsync & nits by @ScarletKuro in #7105
MudForm: Remove child form from parent form on Dispose by @jimitndiaye in #7086
Other Changes
Palette: suppress obsolete warning in our code base by @ScarletKuro in #7078
Localization: sync dependency versions by @ScarletKuro in #7112
New Contributors
@meenzen made their first contribution in #7096
@jimitndiaye made their first contribution in #7086
Full Changelog: v6.5.0...v6.6.0

Version v6.5.0
Released on Haziran 24, 2023

What's Changed
New Features
MudDialog: Add generic DialogParameters by @SSchulze1989 in #7029
MudDataGrid: Add ability to completely use own IFilterDefinition by @ScarletKuro in #7057
SortDefinition: Fix build warning about missing nullable by @ScarletKuro in #7067
MudBlazor.Docs.Wasm: enable nullable by @ScarletKuro in #7068
MudRating: Add nullable annotation. by @ScarletKuro in #7069
MudStack: Add nullable annotation. by @ScarletKuro in #7071
MudRTLProvider: Add nullable annotation. by @ScarletKuro in #7073
MudDataGrid: Add Localization Capabilites by @Meduris in #7024
Bug Fixes
MudNavLink: Fix line-height issue causing UI shift (#5848) by @joe-gregory in #6958
Fix: Generation of DocStrings fails when generic class with same name exists by @SSchulze1989 in #7016
MudSelect: Fix loss of focus to receive key strokes by @ScarletKuro in #7023
ColorPickerTests: Fix flaky test by @ScarletKuro in #7025
PopoverService: Fix DisposeAsync deadlock on WPF/WinForm platforms by @ScarletKuro in #7044
DataGrid: Allow resizing when overflowing with sticky columns by @SinisterMaya in #6991
FileUploadTests: set culture to InvariantCulture to remove dependency on local system culture. by @SSchulze1989 in #7030
ServiceCollectionExtensions: Fix registration with options by @ScarletKuro in #7065
Other Changes
MudExpansionPanels: Add nullable annotation. by @ScarletKuro in #6976
MudProgress: Add nullable annotation. by @ScarletKuro in #6993
Docs: MudDatePicker - Added example for IsDateDisabledFunc and AdditionalDateClassesFunc by @RafBorrelli in #6262
MudImage: Add nullable annotation. by @ScarletKuro in #6997
PopoverService: Move StateHasChanged from responsibility by @ScarletKuro in #6998
MudDateRangePicker: Add support for AdditionalDateClassesFunc by @EricEzaM in #6203
MudSwipeArea: Add nullable annotation. by @ScarletKuro in #7072
MudContainer: Add nullable annotation. by @ScarletKuro in #7075
MudSlider: Add nullable annotation. by @ScarletKuro in #7077
New Contributors
@RafBorrelli made their first contribution in #6262
@joe-gregory made their first contribution in #6958
@SSchulze1989 made their first contribution in #7016
@EricEzaM made their first contribution in #6203
@Meduris made their first contribution in #7024
Full Changelog: v6.4.1...v6.5.0

Version v6.4.1
Released on Haziran 08, 2023

# Hotfix Release
What's Changed
Bug Fixes
PopoverService: Remove ConfigureAwait(false) by @ScarletKuro in #6981
Full Changelog: v6.4.0...v6.4.1

Version v6.4.0
Released on Haziran 07, 2023

What's Changed
New Features
MudColorPicker: Fix/Enable color picker form validation (#6841) by @igotinfected in #6924
MudTable: Add methods to expand or collapse all groups by @RickMcDee in #6812
MudMenuItem: Add AutoClose parameter for controlling closing behavior by @ferraridavide in #6942
PopoverService: New design by @ScarletKuro in #6953
Bug Fixes
Inputs: Fix order of Value and Text changes for increased stability by @mckaragoz in #6900
Other Changes
MudDataGrid: Filterable false removes filter from dropdown in filters panel by @BenjaminGroseclose in #6212
Docs: Correct typos by @Mr-Technician in #6845
New Contributors
@BenjaminGroseclose made their first contribution in #6212
@ferraridavide made their first contribution in #6942
Full Changelog: v6.3.1...v6.4.0

Version v6.3.1
Released on Mayıs 27, 2023

Note: this is a hotfix release mitigating an accidental breaking change in MudAppBar which could have broken your app's layout.
What's Changed
Other Changes
MudDataGrid: Remove unused UnitTests.Viewer by @ScarletKuro in #6928
MudAppBar: Fix default value of WrapContent (#6808, #6869) by @henon in #6929
Full Changelog: 6.3.0...v6.3.1

Version v6.3.0
Released on Mayıs 24, 2023

What's Changed
New Features
ISnackbar: Add RemoveByKey #6857 by @Kumima in #6860
ScrollToTop: Add OnClick event by @i-n-n-k-e-e-p-e-r in #6870
Tabs: Add TabHeaderClass property (#5424) by @gaplin in #6298
MudToolBar: Optional Wrapping and Appbar compatibility by @TimWeidner in #6869
Bug Fixes
MudTimePicker: Fix duplicate hour event in minute mode #6523 by @sho12333 in #6599
PickerToolbar: Fix issue with landscape mode for non-static pickers (#5867) by @lostinmilkshake in #6874
DataGrid: Fix loading style by @ling921 in #6885
New Contributors
@sho12333 made their first contribution in #6599
@Kumima made their first contribution in #6860
@i-n-n-k-e-e-p-e-r made their first contribution in #6870
@lostinmilkshake made their first contribution in #6874
@ling921 made their first contribution in #6885
@TimWeidner made their first contribution in #6869
@adgranger made their first contribution in #6912
Full Changelog: v6.2.5...v6.3.0

Version v6.2.5
Released on Mayıs 17, 2023

What's Changed
New Features
MudButton: Add ability to set custom rel attribute content by @TehGM in #6301
MudAutoComplete: Add BeforeItemsTemplate and AfterItemsTemplate by @derekwelton in #6752
MudPicker: Add OnClick event (#6797) by @wesProg23 in #6798
MudDataGrid: FilterDefinition as interface by @ScarletKuro in #6848
Bug Fixes
Remove Parameter attribute from non-component SnackbarOptions class by @Eagle3386 in #6801
MudTreeView: Don't accept clicks and selection changes for Disabled items by @RickMcDee in #6783
LinearProgress: Fix typo in size parameter docs by @Mr-Technician in #6807
MudDataGrid: Fix runtime exception with PageSizeOptions by @SinisterMaya in #6784
MudDropContainer: Fix bad index when dropping item on itself (#5006) by @ZephyrZiggurat in #6830
MudTable: Fix discrepency with initial RowsPerPage in TablePager by @SinisterMaya in #6776
Other Changes
MudToolBar: Allow longer content by @bdebaere in #6808
MudTable: Avoid NoRecordsContent showing before loading data by @bdebaere in #6811
MudAutocomplete: Fix Value binding not updating by @nefarius in #6805
New Contributors
@TehGM made their first contribution in #6301
@derekwelton made their first contribution in #6752
@bdebaere made their first contribution in #6808
@wesProg23 made their first contribution in #6798
@nefarius made their first contribution in #6805
@ZephyrZiggurat made their first contribution in #6830
**This is a rerelease of 6.2.4 which was removed due to build issues**
Full Changelog: v6.2.3...v6.2.5

Version v6.2.3
Released on Mayıs 03, 2023

What's Changed
Breaking Changes
MudTabs: Fix XSS in Text (#5478) by @elodon in #6680
New Features
Form components: Add ResetAsync / ResetValueAsync by @ScarletKuro in #6693
Dialog: Add ClassBackground Option to Support Blurred Background by @mckaragoz in #6725
MudCollapse: Add nullable annotation and Async AnimationEnd. by @ScarletKuro in #6747
MudDataGrid: Add drag and drop column reordering by @segfault- in #6700
DateRangePicker: Highlight current date by @RickMcDee in #6753
Bug Fixes
MudDataGrid: Fix GroupExpanded when using async data source by @alfadelta88 in #6660
MudMenu: Introduce OnClickHandlerAsync to fix #6645 by @ScarletKuro in #6648
MudDataGrid: Fix GroupExpanded when using ServerData by @alfadelta88 in #6667
MudAutocomplete: Fix the highlight of selected item when Strict is set to false (#6412) by @csombi in #6489
MudOverlay: CA2012 Use ValueTasks correctly by @ScarletKuro in #6670
Bug report template had 5.x.x as placeholder by @TDroogers in #6696
MudSwitch: LabelPosition.Start uses row-reverse instead of RTL by @SinisterMaya in #6638
Docs: Fix typo in toggle example by @SnakyBeaky in #6709
MudAutocomplete: Fix Race Condition #6475 by @ScarletKuro in #6701
BreakpointService & ResizeService: KeyNotFoundException fix #3993 #3356 #3698 by @ScarletKuro in #6727
MudChip, MudChipSet: Await where applicable by @ScarletKuro in #6735
MudDialog: Fix description of the DefaultFocus parameter by @RickMcDee in #6751
MudAppBar: Bottom=true should render
instead of
by @Eagle3386 in #6646
MudPicker: HandleKeyDown was called twice by @RickMcDee in #6777
Other Changes
MudOverlay: Await OnClick event by @BieleckiLtd in #6153
MudFormComponent: Add nullable annotation. by @ScarletKuro in #6608
MudField: Add nullable annotation. by @ScarletKuro in #6642
MudDivider: Add nullable annotation. by @ScarletKuro in #6641
MudButtonGroup: Add nullable annotation. by @ScarletKuro in #6640
MudOverlay: Add nullable annotation. by @ScarletKuro in #6665
MudMainContent: Add nullable annotation. by @ScarletKuro in #6664
MudText: Add nullable annotation. by @ScarletKuro in #6677
MudVirtualize: Add nullable annotation. by @ScarletKuro in #6678
MudSimpleTable: Add nullable annotation. by @ScarletKuro in #6679
MudBooleanInput: Add nullable annotation. by @ScarletKuro in #6681
MudBreakpointProvider: Add nullable annotation. by @ScarletKuro in #6720
MudHighlighter: Add nullable annotation. by @ScarletKuro in #6731
Obsolete ICommand & CommandParameter by @ScarletKuro in #6773
New Contributors
@elodon made their first contribution in #6680
@SnakyBeaky made their first contribution in #6709
@RickMcDee made their first contribution in #6751
@segfault- made their first contribution in #6700
@Eagle3386 made their first contribution in #6646
Full Changelog: v6.2.2...v6.2.3

Version v6.2.2
Released on Nisan 13, 2023

What's Changed
Bug Fixes
Docs: Fix possible NullReferenceException and Cross-Origin Request Blocked by @ScarletKuro in #6647
Full Changelog: v6.2.1...v6.2.2

Version v6.2.1
Released on Nisan 12, 2023

What's Changed
Note: This release fixes the regressions in MudDataGrid but also brings new breaking changes in data grid. It was our last chance to rectify the public API. Many methods are now async and got the ...Async postfix. Should no further regressions arise we'll consider MudDataGrid stable and the API will stay as it is now.
Breaking Changes
DataGrid: Change several public methods from async void to async Task and add Async postfix (Breaking Change) by @ScarletKuro in #6614
MudDataGrid: Rename AddFilterAsync and ClearFiltersAsync (Breaking Change) by @ScarletKuro in #6639
New Features
MudDialog: Change OnBackdropClick Action to EventCallback by @BieleckiLtd in #6151
Theming: Make all Palette properties virtual by @TDroogers in #6519
MudAlert: Add nullable annotation. by @ScarletKuro in #6539
MudAppBar: Add nullable annotation by @ScarletKuro in #6540
MudBadge: Add nullable annotation. by @ScarletKuro in #6554
MudComponentBase: Add explicit IMudStateHasChange.StateHasChanged by @ScarletKuro in #6563
MudDropZone: Add nullable annotation and fix #6551, #4695. by @ScarletKuro in #6561
FileUpload: Add Disabled property by @Mr-Technician in #6572
MudTreeViewItem: Add 'BodyContent' which renders instead of text, end text and end icon. by @rpc-scandinavia in #3831
MudForm: Add ReadOnly and Disabled properties by @Mr-Technician in #6511
MudSwipeArea: SwipeEventArgs Feature by @mckaragoz in #6574
MudDataGrid: Allow user to use a custom IComparer for sorting by @SinisterMaya in #6368
MudDataGrid: Add ability to use SortBy on TemplateColumn. by @tjscience in #6613´
Bug Fixes
MudTable: Add filtered items cache for each render. by @adameste in #6470
MudDataGrid: Improve nullability, encapsulation and some nits by @ScarletKuro in #6421
MudDataGrid: Sorting and filtering performance improvements. by @adameste in #6495
BaseInput: Validation should not run on blur when ReadOnly = true. by @Mr-Technician in #6518
Fixes MudDataGrid NullReferenceExeption by @alfadelta88 in #6541
MudDataGrid: Make AggregateDefinition work with nullable values and cover with tests by @ScarletKuro in #6515
MudDatagrid: Fix grouping to work with filtered items cache. by @adameste in #6557
MudDrawer: Fix #6154 by not re-rendering on mouse events by @ScarletKuro in #6575
MudPopover: Fix flickering in delayed bss (#6345) by @ScarletKuro in #6579
Autocomplete: Should not have a Clear button when ReadOnly by @Mr-Technician in #6596
MudDataGrid: Fix ExpandAllGroups/CollapseAllGroups skipping some groups (#6591) by @alfadelta88 in #6592
MudDataGrid: Fix GroupExpanded not working by @alfadelta88 in #6604
MudDataGrid: Filter for Nullable DateTime not working(#6521) by @ScarletKuro in #6607
PopoverService: Fix DisposeAsync causing hangs on MAUI by @Mr-Technician in #5367
MudBaseInput: Make OnBlurred async by @ScarletKuro in #6633
MudBaseInput: Add InvokeKeyDownAsync / InvokeKeyUpAsync deprecate InvokeKeyDown / InvokeKeyUp by @ScarletKuro in #6636
MudDataGrid: Fix filtering modes when using ServerData by @alfadelta88 in #6627
MudFormComponent: Deprecate BeginValidate in favor of Async version by @ScarletKuro in #6637
Other Changes
MudAvatar: Add nullable annotation by @ScarletKuro in #6543
MudIcon: Add nullable annotation. by @ScarletKuro in #6578
MudDialogInstance: return back public API ForceRender by @ScarletKuro in #6588
New Contributors
@adameste made their first contribution in #6470
@rpc-scandinavia made their first contribution in #3831
@KBM-Kevin-Kessler made their first contribution in #6542
Full Changelog: v6.2.0...v6.2.1

Version v6.2.0
Released on Mart 15, 2023

What's Changed
Breaking Changes
MudDataGrid: Configuration using expressions by @tjscience in #6049
New Features
MudTabs: Optionally cancel tab panel activations by @Flaflo in #4738
Avatar: Refactor Image support by @Mr-Technician in #6382
MudDropZone: Add support for Chromium based browsers on Mobile by @emorell96 in #4818
SourceCodeGenerator: Add support for enum source code generation by @xC0dex in #6306
ThemeProvider: Allow watching dark/light system setting by @TDroogers in #6320
RadioGroup: Add ReadOnly and Disabled options. by @Mr-Technician in #6418
MudDatePicker: Add disabled functionality for months only picker by @ingkor in #6438
MudDatePicker: Add disabling months if day is fixed (#6336) by @gaplin in #6338
Docs: Fix release page and roadmap page by @Garderoben in #6455
Bug Fixes
MudPicker: Fix flickering in delayed BSS. (#5450) by @gaplin in #6344
MudCheckbox: Use flex-row-reverse instead of mud-rtl for label positioning by @Flaflo in #6377
MudTable: Fix V3022 warning by @HClausing in #6374
Dialog: Fix issues with nested inline Dialogs by @Mr-Technician in #6390
MudCarousel: Fix wrong transition direction when using SelectedIndex (#6392) by @gaplin in #6394
MudMenu: Fix Touch Bubbling Which Causes Other Unwanted Click Event Firing on Mobile Devices by @mckaragoz in #6358
Fix analyzer NullReferenceException in old ToDescriptionString, add test scanrio for it. by @ScarletKuro in #6423
Datagrid: Fix Dialog by @Mr-Technician in #6415
MudDataGrid : Fixes Select All when using ServerData by @alfadelta88 in #5999
MudTable: Show loading indicator whenever table loads data from server by @sensslen in #4665
ThemeProvider: Trimming and minor formatting fix by @ScarletKuro in #6444
MudDataGrid: Fix missing content/error in Cell edit mode for HierarchyColumn (#6024) by @HClausing in #6441
Autocomplete: Fix Autocomplete opening on Reset by @Mr-Technician in #6387
MudAutocomplete: Fix KeyDown Event by @ingkor in #6462
Other Changes
Autocomplete: Correct summary for Strict by @rusfield in #6396
Update README.md by @mckaragoz in #6339
Fix .csproj formatting by @ScarletKuro in #6422
Fix code formatting in MudDropZone.razor and MudDynamicDropItem.razor by @ScarletKuro in #6425
Avatar: Fix obsolete workaround for images. by @Mr-Technician in #6448
MudBaseInput: Obsolete OnKeyPress, to be removed in v7 by @henon in #6464
New Contributors
@rusfield made their first contribution in #6396
@emorell96 made their first contribution in #4818
@xC0dex made their first contribution in #6306
@alfadelta88 made their first contribution in #5999
@dev-jlb made their first contribution in #6447
@ingkor made their first contribution in #6438
Full Changelog: v6.1.9...v6.1.10

Version v6.1.9
Released on Şubat 15, 2023

What's Changed
New Features
MudAutocomplete: Add option to search while an option is selected by @Mr-Technician in #6231
MudTabPanel: Add ShowCloseIcon property by @Yomodo in #6197
MudThemeProvider: Add sudo css selector for generated theme (5781) by @stoepa in #5798
MudButton: Add IconSize property by @Yomodo in #6040
MudSwitch: Add Size Parameter by @SinisterMaya in #5673
MudSwitch: Show Validation ErrorText (#6247) by @igotinfected in #6268
Bug Fixes
Tabs: Fix disabling prevButton after upsizing (#6235) by @gaplin in #6236
Fix NullReferenceException in ResizeOptions equality logic #6128 by @henon in #6308
Tabs: Fix scroll buttons not working on low width (#5212) by @gaplin in #6304
MudDialog: CSS fix for scroll when size is greater than view height by @enkodellc in #4132
Other Changes
InputBase: Add ForceUpdate (fixes #5132) by @mckaragoz in #6266
ResizeObserver: fix typo in directory name by @Habetdin in #6303
New Contributors
@stoepa made their first contribution in #5798
@gaplin made their first contribution in #6236
@Habetdin made their first contribution in #6303
@enkodellc made their first contribution in #4132
Full Changelog: v6.1.8...v6.1.9

Version v6.1.8
Released on Ocak 27, 2023

What's Changed
New Features
MudTable: Enable FluentValidation for TableRowValidator. by @Mr-Technician in #6193
MudInput: Add Accessibility Titles to Nested Icons by @WalterWillis in #5531
Bug Fixes
MudAutocomplete: Fix TextChanged event not sending the text by @idan-h in #6120
MudTable: Fix 2nd level checkbox default indeterminate state by @Yomodo in #6075
Change background of error when circuit (connection) is lost. by @BieleckiLtd in #6116
MudDivider: Fix fullwidth behavior in flex containers by @mckaragoz in #5472
MudTooltip: Fix Style not forwarded to popover by @rogue6800 in #6051
MudTable: Add null check on server data items in FilteredItems by @Mr-Technician in #6186
MudTabs: Fix incorrect ActivePanelIndex by @Yomodo in #6100
Other Changes
MudDateRangePicker: Fix DateRangePicker validation in MudForm (#6194) by @igotinfected in #6195
New Contributors
@idan-h made their first contribution in #6120
@WalterWillis made their first contribution in #5531
@LinuxDoku made their first contribution in #6225
Full Changelog: v6.1.7...v6.1.8

Version v6.1.7
Released on Ocak 02, 2023

What's Changed
New Features
MudDialog: Fix Unselectable Content Text by @mckaragoz in #5355
MudTable: Implement "All" option for MudTablePager. by @Mr-Technician in #5994
Icons: Update material icons by @QianMoXi in #6034
MudTabs: Add MinimumTabWidth property by @rogue6800 in #6044
Bug Fixes
Icons: Remove any
Version v6.1.6
Released on Aralık 18, 2022

What's Changed
Breaking Changes
MudTable: Fixed checkbox state for MultiSelect and (Multi)Grouping by @Yomodo in #5985
If you were using the public properties `bool IsChecked` on the grouping or header/footer rows they had a bug in that they only showed `true` or `false` even if there was only a partial selection. In order to fix this bug the properties were changed to `bool? IsChecked`. The `null` value representing the indeterminate state. We released this breaking change in our normal patch revision as it fixes incorrect behaviour. Users that have been using the incorrect values may need to adjust their code to cope with the new correct datatype and values.
New Features
MudTable: Allow row-click without selecting checkbox when in multi-select mode by @Yomodo in #5961
Make Blazor Error UI message more legible by @BieleckiLtd in #5996
MudTable: Added OverscanCount as a parameter. by @Suenodk in #5990
Bug Fixes
MudProgress: Fixed border radius for rounded linear progress bar by @nmakhmutov in #5963
BreakpointService: IsMediaSize should will return true for Breakpoint.Always by @pars-dev in #5898
MudAutoComplete: Don't write to console for errors by @mikes-gh in #5970
MudTable: Fix issue where sub-tables in the last row of a table would not have borders by @Mr-Technician in #5992
MudDataGrid: Fix CollapseAll not clearing internal variable by @snakex64 in #5995
MudTable: Toggle multigrouping checkbox ocassionaly flickers by @Yomodo in #6003
New Contributors
@pars-dev made their first contribution in #5898
@Suenodk made their first contribution in #5990
Full Changelog: v6.1.5...v6.1.6

Version v6.1.5
Released on Aralık 11, 2022

What's Changed
New Features
MudSelect and MudAutocomplete: Add ListClass by @mckaragoz in #5402
MudTreeViewItem: add CanExpand Property (#4093) by @LennartKleymann in #4097
Bug Fixes
MudTable: SelectedItem should fire regardless of EditTrigger setting. by @Mr-Technician in #5903
MudPopover : Check popoverContentNode null before use by @Stuart88 in #5897
Tests: Fix IDialog interface so it can be Moqed by @mikes-gh in #5925
MudSnackbar: Fix overflowing, long message (#3945) by @BieleckiLtd in #5929
MudDataGrid: fixed the resizer handle visibility by @SinisterMaya in #5851
MudDialog: Fix not aligned Toolbar buttons on Dialog header (#5497) by @m4ss1m0g in #5725
MudDialog: Fix close button in RTL by @mikes-gh in #5951
MudDataGrid: Fix the FilterHeaderCell visible when the Column is hidden by @nicoarm93 in #5777
MudDataGrid: ServerData initialization fix by @creeve-qci in #5635
MudAlert: Fix CloseIcon color in Filled Variant (#5436) by @fondraco in #5449
New Contributors
@Stuart88 made their first contribution in #5897
@SinisterMaya made their first contribution in #5851
@m4ss1m0g made their first contribution in #5725
@fondraco made their first contribution in #5449
@LennartKleymann made their first contribution in #4097
Full Changelog: v6.1.4...v6.1.5

Version v6.1.4
Released on Aralık 05, 2022

What's Changed
New Features
MudLink: Add OnClick parameter (#1518) by @BieleckiLtd in #5785
Charts: Make Legend Clickable by @belucha in #5484
MudTreeView: Add Public Select Method and Disable Text Selection When ExpandOnDoubleClick is true by @mckaragoz in #5856
MudBaseButton: Make OnClickHandler virtual by @CodeLifterIO in #5879
MudTooltip: Activation via Hover, Focus or Click by @mckaragoz in #4647
Bug Fixes
MudSnackbar: Fix memory leak upon Dispose by @Yomodo in #5706
MudColorPicker: Fix MaxLength on HEX TextField for transparent colors by @DoobieAsDave in #5582
ScrollManager: Fix exceptions when no JS environment available by @mikes-gh in #5770
MudDialog: Fix Dialog is null in IDialogReference by @ScarletKuro in #5101
MudCarousel: Fix swipe direction for next/previous in rtl by @abduwaris in #5811
MudRadio: Fix content placement in rtl by @abduwaris in #5813
MudTable: Fix inconsistent row border thickness by @dennml in #5823
MudTable: Fixed table grouping item selection issue (#5759) by @Gopichandar in #5760
MudChip: Fix pointer cursor if href set by @mdekok in #5871
MudFileUpload: Maximum file count integrated as a parameter by @ummerland in #5861
MudTimePicker: Fix TimePicker validation in MudForm (#5883) by @igotinfected in #5884
MudTreeView: Fix Closing Arrow Transition by @mckaragoz in #5858
New Contributors
@rena0157 made their first contribution in #5767
@BieleckiLtd made their first contribution in #5785
@DoobieAsDave made their first contribution in #5582
@abduwaris made their first contribution in #5811
@CodeLifterIO made their first contribution in #5879
@ummerland made their first contribution in #5861
@igotinfected made their first contribution in #5884
@popandepo made their first contribution in #5893
Full Changelog: v6.1.2...v6.1.4

Version v6.1.3-dev.1
Released on Kasım 22, 2022

What's Changed
MudSnackbar: Fix memory leak upon Dispose by @Yomodo in #5706
MudLink: Add OnClick parameter (#1518) by @BieleckiLtd in #5785
Charts: Make Legend Clickable by @belucha in #5484
MudColorPicker: Fix MaxLength on HEX TextField for transparent colors by @DoobieAsDave in #5582
ScrollManager: Fix exceptions when no JS environment available by @mikes-gh in #5770
MudDialog: Fix Dialog is null in IDialogReference by @ScarletKuro in #5101
Fix ResizeBasedService trim warning by @ScarletKuro in #5809
New Contributors
@rena0157 made their first contribution in #5767
@BieleckiLtd made their first contribution in #5785
@DoobieAsDave made their first contribution in #5582
Full Changelog: v6.1.2...v6.1.3-dev.1

Version v6.1.2
Released on Kasım 14, 2022

What's Changed
MudMenu: Fix MouseEventArgs in net7 on BSS by @mikes-gh in #5738
Build: Fix missing static assets in nuget by @mikes-gh in #5751
Full Changelog: v6.1.0...v6.1.2

Version v6.1.0
Released on Kasım 13, 2022

Announcement
This package is our first official package that contains both net6 and net7 native versions of MudBlazor 🎉 Your app will automatically select the appropriate framework version.
What's Changed
Snackbar: Fix accidental API break by PR #5310 by @henon in #5701
Snackbar: Fix break due to trimming of SnackbarMessageType by @mikes-gh in #5711
Build: add net7 library (multi-targeting) by @mikes-gh in #5713
Fix MudDivider consistent thickness by @dennml in #5491
Tests: Fix locale issue due to date literal by @mikes-gh in #5729
MudDataGrid: Add GroupClassFunc and GroupStyleFunc by @franklupo in #5560
MudDataGrid: Fix missing DataGrid reference in FilterDefinition by @nicoarm93 in #5498
New Contributors
@dennml made their first contribution in #5491
@franklupo made their first contribution in #5560
Full Changelog: v6.0.18...v6.1.0

---

## Additional Resources

- **Official Documentation**: https://mudblazor.com/
- **GitHub Repository**: https://github.com/MudBlazor/MudBlazor
- **Migration Guides**:
  - [v7 → v8 Migration](https://github.com/MudBlazor/MudBlazor/issues/8447)
  - [v6 → v7 Migration](https://github.com/MudBlazor/MudBlazor/issues/8447)
- **Breaking Changes Summary**: https://github.com/MudBlazor/MudBlazor/releases

---

## AI Assistant Usage Guidelines

1. **Always verify version** before generating code
2. **Search this document** for feature availability
3. **Warn about breaking changes** when suggesting upgrades
4. **Use version-specific syntax** in code examples
5. **Reference specific version numbers** in explanations

This document is maintained to help AI assistants generate accurate, version-appropriate MudBlazor code.

<!-- End of MudBlazor Version Instructions -->
