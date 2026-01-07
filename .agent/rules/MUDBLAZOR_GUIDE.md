---
trigger: model_decision
description: If you are on a mudblazor component always remember that we are using v8.15 and use this file.
---

# MudBlazor v7+ Development Guide

> **Reference**: [Official Documentation](https://mudblazor.com/docs/overview)

This guide provides the essential patterns for using MudBlazor in this project, specifically targeting v7+ changes and best practices.

## 1. Dialogs (`MudDialog`)

### Key Changes (v7)
- **Interface**: Use `IMudDialogInstance` instead of `MudDialog` or `IDialogReference` inside the dialog component.
- **Reference**: The `[CascadingParameter]` is now typed as `IMudDialogInstance`.

### Pattern
**MyDialog.razor.cs**:
```csharp
public partial class MyDialog
{
    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } // v7 Standard

    private void Submit()
    {
        MudDialog.Close(DialogResult.Ok(true));
    }

    private void Cancel()
    {
        MudDialog.Cancel();
    }
}
```

**Opening a Dialog**:
```csharp
var options = new DialogOptions { CloseOnEscapeKey = true };
var dialog = await DialogService.ShowAsync<MyDialog>("Title", parameters, options);
var result = await dialog.Result;

if (!result.Canceled)
{
    // Handle success
}
```

## 2. Data Grids (`MudDataGrid`)
Prefer `MudDataGrid<T>` over `MudTable<T>` for advanced features (filtering, sorting).

### Pattern
```razor
<MudDataGrid T="Category" Items="@_categories" Filterable="true" SortMode="SortMode.Multiple">
    <Columns>
        <PropertyColumn Property="x => x.Id" Title="ID" />
        <PropertyColumn Property="x => x.Name" Title="Name" />
        <TemplateColumn Title="Actions">
            <CellTemplate>
                <MudIconButton Icon="@Icons.Material.Filled.Edit" OnClick="@(() => Edit(context.Item))" />
            </CellTemplate>
        </TemplateColumn>
    </Columns>
    <PagerContent>
        <MudDataGridPager T="Category" />
    </PagerContent>
</MudDataGrid>
```

## 3. Forms & Validation
Use `MudForm` only for simple scenarios. For business logic, prefer `EditForm` with `FluentValidation`.

### FluentValidation Integration
```razor
<MudForm @ref="_form" @bind-IsValid="@_success">
    <MudTextField @bind-Value="model.Name" 
                  Label="Name" 
                  For="@(() => model.Name)" /> 
</MudForm>
```
*Note: The `For` attribute connects the field to the validator.*

## 4. UI Thread & Async Updates
When updating UI state after an async background operation (like a Dialog result), ensure you are on the UI thread.

```csharp
// Safe Pattern
await InvokeAsync(() => {
    StateHasChanged();
});
```

## 5. Icons
Use `Icons.Material.Filled` or `Icons.Material.Outlined`. Avoid string-based icon names if possible to benefit from compile-time checks.
