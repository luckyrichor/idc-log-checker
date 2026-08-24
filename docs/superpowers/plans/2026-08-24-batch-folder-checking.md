# Batch Folder Checking Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Upgrade both GUI programs from one-folder scanning to ordered multi-folder selection and drag-and-drop with master-detail results and two report scopes.

**Architecture:** `Checker.Core` gains UI-independent input normalization, sequential batch orchestration, aggregate statistics, selection priority, and batch reporting. Avalonia and WinForms bind to the same batch result semantics; their only platform-specific responsibilities are folder picking, drag events, and rendering controls.

**Tech Stack:** .NET 10, C# 13, Avalonia 12.1.1, Windows Forms, xUnit 2.9.3.

**Spec:** `docs/superpowers/specs/2026-08-24-batch-folder-checking-design.md`

## Global Constraints

- Preserve the embedded 62-device and 3,660-TXT baseline and all existing error/warning semantics.
- Scan source folders read-only and sequentially in user-supplied order.
- New selection or drop replaces the previous batch; a wholly invalid input leaves the previous batch unchanged.
- Both Windows outputs remain self-contained single-file `win-x64` executables requiring no installed runtime.
- Avalonia and WinForms must not duplicate directory or TXT validation rules.
- Windows-only picker and drag behavior must be recorded as pending Windows 11 physical validation.

---

### Task 1: Normalize multiple input paths

**Files:**
- Create: `src/Checker.Core/Batch/BatchInputNormalizer.cs`
- Create: `src/Checker.Core/Batch/BatchInputResult.cs`
- Test: `tests/Checker.Tests/BatchInputNormalizerTests.cs`

**Interfaces:**
- Produces: `BatchInputResult BatchInputNormalizer.Normalize(IEnumerable<string?> paths)` with ordered `ValidPaths`, `SkippedItems`, `DuplicatePaths`, and `HasValidPaths`.

- [ ] Write failing tests proving valid order is retained, duplicates are removed using platform path comparison, files and missing paths are skipped with Chinese reasons, mixed input keeps valid folders, and wholly invalid input has no valid paths.
- [ ] Run `dotnet test ... --filter FullyQualifiedName~BatchInputNormalizerTests` and confirm compilation fails because the batch types do not exist.
- [ ] Implement canonical full-path normalization, directory existence checks, duplicate detection, and readable skipped-item records without changing the filesystem.
- [ ] Run the targeted tests and all existing core tests; confirm they pass without warnings.
- [ ] Commit input normalization.

### Task 2: Sequential batch coordinator and aggregate presentation

**Files:**
- Create: `src/Checker.Core/Batch/BatchScanCoordinator.cs`
- Create: `src/Checker.Core/Batch/BatchScanProgress.cs`
- Create: `src/Checker.Core/Batch/BatchScanResult.cs`
- Create: `src/Checker.Core/Presentation/BatchResultPresentation.cs`
- Test: `tests/Checker.Tests/BatchScanCoordinatorTests.cs`
- Test: `tests/Checker.Tests/BatchResultPresentationTests.cs`

**Interfaces:**
- Produces: `Task<BatchScanResult> BatchScanCoordinator.ScanAsync(IReadOnlyList<string> paths, IProgress<BatchScanProgress>? progress, CancellationToken token)`.
- Produces: aggregate counts for clean, warning, failed, total errors, and total warnings.
- Produces: `BatchResultPresentation.DefaultSelectedIndex` selecting first failed, then first warning, then first item.

- [ ] Write failing tests using small real directory fixtures to prove input order, continued scanning after a failed item, aggregate counts, progress folder indexes, and default selection priority.
- [ ] Run targeted tests and confirm expected missing-type failures.
- [ ] Implement sequential calls to the existing `DirectoryScanner`; translate directory progress into batch progress and never duplicate scan rules.
- [ ] Implement immutable aggregate and presentation records with Chinese status text and shared status colors.
- [ ] Run targeted tests and the full suite; confirm all pass.
- [ ] Commit batch coordination.

### Task 3: Batch Chinese report

**Files:**
- Create: `src/Checker.Core/Reporting/ChineseBatchReportWriter.cs`
- Test: `tests/Checker.Tests/ChineseBatchReportWriterTests.cs`

**Interfaces:**
- Produces: `string ChineseBatchReportWriter.Write(BatchScanResult result)` and `SaveAsync(...)`.
- Consumes: the existing `ChineseTextReportWriter.Write(ScanResult)` for each folder section.

- [ ] Write failing tests for batch summary, skipped/duplicate input notes, ordered folder sections, clean/warning/error conclusions, and UTF-8 save output.
- [ ] Run targeted tests and confirm the writer is missing.
- [ ] Implement a concise batch header and clearly separated per-folder sections while reusing the existing single-result writer.
- [ ] Run targeted tests and full core tests.
- [ ] Commit batch report generation.

### Task 4: Avalonia batch view model, multi-select, and drag-and-drop

**Files:**
- Modify: `src/Checker.Avalonia/MainWindowViewModel.cs`
- Modify: `src/Checker.Avalonia/MainWindow.axaml`
- Modify: `src/Checker.Avalonia/MainWindow.axaml.cs`
- Create: `src/Checker.Avalonia/FolderResultViewModel.cs`
- Test: `tests/Checker.Tests/AvaloniaBatchViewModelTests.cs`

**Interfaces:**
- Produces: `ReplaceSelection(IEnumerable<string?> paths)`, `RunBatchScanAsync(...)`, `SelectFolder(FolderResultViewModel?)`, batch counters, folder result collection, current detail, and two export capabilities.
- Consumes: Avalonia `OpenFolderPickerAsync` with `AllowMultiple=true` and `DragDrop` file data.

- [ ] Write failing view-model tests for replacement, wholly invalid preservation, mixed-input message, running state, aggregate counters, default selected folder, folder switching, filtering, and export availability.
- [ ] Run targeted tests and confirm failures occur because batch view-model APIs are absent.
- [ ] Implement view-model behavior over the core coordinator and presentation types.
- [ ] Replace the single-path layout with batch summary plus left folder list and right current-result details; add two export buttons and a drag overlay.
- [ ] Implement multi-select picker, drag-over/drop validation, batch/current report save, copy, and open-location handlers.
- [ ] Build and run view-model/full tests with zero warnings.
- [ ] Commit Avalonia batch UI.

### Task 5: WinForms native multi-folder picker and batch master-detail UI

**Files:**
- Create: `src/Checker.WinForms/NativeMultiFolderPicker.cs`
- Create: `src/Checker.WinForms/BatchFormController.cs`
- Modify: `src/Checker.WinForms/MainForm.cs`
- Modify: `src/Checker.WinForms/MainForm.Designer.cs`
- Test: `tests/Checker.Tests/BatchFormControllerTests.cs`

**Interfaces:**
- Produces: pure `BatchFormController` state used by tests and WinForms rendering.
- Produces: Windows COM `IFileOpenDialog` wrapper using `FOS_PICKFOLDERS | FOS_ALLOWMULTISELECT | FOS_FORCEFILESYSTEM`.
- Consumes: `BatchInputNormalizer`, `BatchScanCoordinator`, and shared presentations.

- [ ] Write failing controller tests for replacement, aggregate folder rows, selected-result switching, shared issue filters, and export state.
- [ ] Run targeted tests and confirm controller is missing.
- [ ] Implement the controller without referencing WinForms controls so tests run on macOS.
- [ ] Implement native multi-folder picker COM declarations and return selected filesystem paths.
- [ ] Enable `AllowDrop`, process `FileDrop`, add a left folder result list, update the right detail panel, and provide current/all report buttons.
- [ ] Cross-build the full solution and run all controller/core tests with zero warnings.
- [ ] Commit WinForms batch UI.

### Task 6: Regression fixtures, Mac smoke test, and Windows republish

**Files:**
- Modify: `test/README.txt`
- Modify: `test/results/avalonia-mac-smoke-test.txt`
- Modify: `test/results/winforms-cross-build.txt`
- Modify: `test/results/release-verification.txt`
- Modify: `发布版/使用说明.txt`
- Regenerate: `发布版/SHA256.txt`

**Interfaces:**
- Produces: refreshed Windows single-file EXEs and auditable verification evidence.

- [ ] Run the full Release test suite and both real-source read-only verification tests.
- [ ] Publish the macOS Avalonia `.app`, launch it, drag multiple generated folders, scan them, switch selected results, and visually inspect batch summary/detail layout.
- [ ] Publish both `win-x64` projects as self-contained single EXEs.
- [ ] Verify PE32+ GUI x86-64 format, embedded baseline resource, file sizes, and generated SHA-256.
- [ ] Regenerate the manifest from both real sources and compare it byte-for-byte with the embedded manifest.
- [ ] Update user instructions and platform-boundary evidence; scan for placeholders and run `git diff --check`.
- [ ] Run the complete test suite once more after final source changes and commit the finished feature.
