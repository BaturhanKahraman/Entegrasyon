# Import/Export Improvement Plan

## Current Architecture Summary

**Key files:**
- `Application/Entegrasyon.Business/Concrete/BulkOperations/BulkOperationManager.cs` -- Main manager (632 lines, 3 import + 3 export + template + log methods)
- `Application/Entegrasyon.Business/Concrete/BulkOperations/ExcelParser.cs` -- ClosedXML parser (174 lines)
- `Application/Entegrasyon.Business/Concrete/BulkOperations/ProductImportValidator.cs` -- Basic validation (84 lines)
- `Application/Entegrasyon.Entity/Dtos/BulkOperations/ExportFilterDto.cs` -- Filter DTO (CategoryId, BrandId, BranchOfficeId, IncludeDeleted only)
- `Application/Entegrasyon.Blazor/Features/BulkOperations/` -- 6 Blazor files (page + 2 dialogs, each with code-behind)
- `Test/Entegrasyon.Test/BulkOperations/` -- 11 unit tests (7 manager + 4 parser)

---

## Step 1: Date Range Filter (Export) -- Priority: HIGH, Effort: SMALL

**Changes:**
1. `ExportFilterDto.cs` -- Add `DateTimeOffset? DateFrom = null` and `DateTimeOffset? DateTo = null` (backward compatible default params)
2. `BulkOperationManager.cs` -- Add `.Where(pv => pv.Product.CreatedAt >= filter.DateFrom.Value)` to all 3 export queries (Products, Prices, Stock)
3. `BulkOperationsPage.razor` -- Add `MudDateRangePicker` to the export tab. Optionally extract `ExportFilterPanel.razor(.cs)` component.

**Tests:** 3 new unit tests in `BulkOperationManagerTests.cs` (one per export type with date filtering)

**Risk:** Low. No breaking changes.

---

## Step 2: CSV Format -- Priority: MEDIUM, Effort: MEDIUM, Depends on Step 1

**Changes:**
1. New `Entity/BulkOperations/ExportFormat.cs` enum (Excel=1, Csv=2)
2. `ExportFilterDto.cs` -- Add `ExportFormat Format = ExportFormat.Excel`
3. New `Business/Concrete/BulkOperations/CsvParser.cs` -- Mirrors ExcelParser's 3 parse methods but for CSV
4. New `Business/Concrete/BulkOperations/CsvWriter.cs` -- Generic CSV writer
5. `BulkOperationManager.cs` -- Route to correct parser by file extension; route export by `filter.Format`
6. `ImportDialog.razor` -- Update `Accept` to `.xlsx,.xls,.csv`
7. `BulkOperationsPage.razor(.cs)` -- Add format selector (MudSelect/MudToggleGroup)
8. `ApplicationDependencyExtension.cs` -- Register CsvParser/CsvWriter

**Tests:** New `CsvParserTests.cs` (4+ tests), CSV import/export tests in manager tests

**Risk:** Medium. CSV encoding (UTF-8 BOM for Turkish chars), multiline value quoting. Consider CsvHelper NuGet vs manual parsing.

---

## Step 3: Column Selection (Export) -- Priority: MEDIUM, Effort: MEDIUM, Depends on Steps 1+2

**Changes:**
1. New `Entity/Dtos/BulkOperations/ExportColumnDto.cs` record
2. `ExportFilterDto.cs` -- Add `List<string>? SelectedColumns = null` (null = all columns)
3. `BulkOperationManager.cs` -- Define column maps per export type, dynamic header/data writing in `WriteToExcelSheets`
4. `IBulkOperationManager.cs` -- New `GetAvailableColumnsAsync(BulkOperationType)` method
5. New `ExportColumnSelector.razor(.cs)` component (MudChipSet or checkboxes with select all/clear)
6. Integrate into `BulkOperationsPage.razor(.cs)`

**Tests:** 3 new unit tests (selected columns only, null = all, available columns list)

**Risk:** Medium. Requires refactoring `WriteToExcelSheets` generic callback.

---

## Step 4: Progress Indicator -- Priority: HIGH, Effort: MEDIUM-LARGE, Independent

**Changes:**
1. New `Entity/Dtos/BulkOperations/BulkOperationProgressDto.cs`
2. New `Business/Channels/Events/BulkOperations/BulkOperationProgressEvent.cs`
3. `BulkOperationManager.cs` -- Add `IProgress<BulkOperationProgressDto>?` to import methods, report every N rows (100)
4. SignalR integration via existing `NotificationHub` or `EventChannel` pattern
5. `ImportDialog.razor(.cs)` -- Replace `Indeterminate` MudProgressLinear with real progress (Value/Max)

**Tests:** Unit tests for progress event publishing at correct intervals

**Risk:** Medium-High. Product import uses PostgreSQL COPY which runs as a single operation -- progress must be milestone-based (parse/validate/execute phases) rather than per-row. SignalR integration complexity.

---

## Step 5: Cancel -- Priority: HIGH, Effort: MEDIUM, Depends on Step 4

**Changes:**
1. `BulkOperationStatus.cs` -- Add `Cancelled = 6`
2. `IBulkOperationManager.cs` -- Add `CancellationToken cancellationToken = default` to all import/export methods
3. `BulkOperationManager.cs` -- Pass cancellationToken to all async calls, add `ThrowIfCancellationRequested()` in loops, update log status on cancel
4. `ImportDialog.razor(.cs)` -- Add `CancellationTokenSource`, wire cancel button, catch `OperationCanceledException`
5. `BulkOperationsPage.razor(.cs)` -- Same for export operations

**Tests:** 3 unit tests (cancelled before start, cancelled midway, export cancellation)

**Risk:** Medium. NpgsqlBinaryImporter COPY cancellation is limited -- wrap in transaction for rollback. Partial import consistency requires explicit transaction management.

---

## Step 6: Enhanced Validation -- Priority: HIGH, Effort: MEDIUM, Independent

**Changes:**
1. New DTOs: `BulkValidationResultDto`, `BulkValidationWarningDto`
2. `ProductImportValidator.cs` -- Expand rules: barcode format (8-13 digits only), valid VAT rates (0/1/10/20), title max length, SalePrice <= ListPrice (warning), CostPrice <= SalePrice (warning)
3. New FluentValidation validators: `ProductImportRowValidator.cs`, `PriceImportRowValidator.cs`, `StockImportRowValidator.cs`
4. `IBulkOperationManager.cs` -- New `ValidateImportAsync(Stream, fileName, type)` for validation-only mode
5. `ImportDialog.razor(.cs)` -- Two-step flow: (1) Select file + "Validate" button -> (2) Show results + "Import" button
6. New `ValidationPreviewPanel.razor(.cs)` component

**Tests:** New `ProductImportValidatorTests.cs` with per-rule tests (barcode format, VAT rate, price logic, length limits)

**Risk:** Low-Medium. FluentValidation is already used in the project. DB-based validation (barcode uniqueness check) requires async, which changes the sync validator pattern.

---

## Recommended Implementation Order

| Order | Step | Reason |
|-------|------|--------|
| 1 | Date Range Filter | Smallest change, backward compatible, quick win |
| 2 | Enhanced Validation | Directly improves import quality, high user impact |
| 3 | Cancel | CancellationToken infrastructure needed for Progress |
| 4 | Progress Indicator | Meaningful only with Cancel support |
| 5 | CSV Format | New format, benefits from existing filter changes |
| 6 | Column Selection | Most complex UI change, builds on all previous steps |

## File Summary

**11 existing files to modify**, **15 new files to create** (5 Entity DTOs/enums, 6 Business classes, 3 Blazor components, 2 test files). All new interface parameters use default values to maintain backward compatibility. No breaking changes to existing callers.
