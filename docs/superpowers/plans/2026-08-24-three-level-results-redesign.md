# Three-level results redesign implementation plan

**Goal:** Deliver the approved Windows 11 UI in both Avalonia and WinForms, with appendable multi-folder selection and error-only three-level result details.

**Architecture:** Keep the scanner and its conservative content rules unchanged. Add a shared presentation layer that assigns every error to one of three inspection levels; both desktop frontends consume the same level summaries and rows. Keep non-error findings in scan/report data for audit compatibility, but exclude them from the on-screen result detail.

**Tech stack:** .NET 10, Avalonia 11, Windows Forms, xUnit, self-contained win-x64 single-file publishing.

---

### Task 1: Shared three-level presentation

**Files:**
- Modify: `src/Checker.Core/Presentation/ResultPresentation.cs`
- Modify: `src/Checker.Core/Presentation/BatchResultPresentation.cs`
- Test: `tests/Checker.Tests/ResultPresentationTests.cs`
- Test: `tests/Checker.Tests/BatchResultPresentationTests.cs`

Add a level enum and map directory-name errors, TXT-name errors, and TXT-content/read errors to the approved levels. Expose error counts, card text, explanatory text, and error-only rows. Verify the exact no-error wording for levels two and three.

### Task 2: Batch selection behavior

**Files:**
- Modify: `src/Checker.Avalonia/MainWindowViewModel.cs`
- Modify: `src/Checker.Avalonia/FolderResultViewModel.cs`
- Modify: `src/Checker.WinForms/BatchFormController.cs`
- Test: `tests/Checker.Tests/AvaloniaBatchViewModelTests.cs`
- Test: `tests/Checker.Tests/BatchFormControllerTests.cs`

Support add-and-deduplicate, remove one folder, clear all and return to the home state. Keep invalid additions from changing the existing selection.

### Task 3: Avalonia approved page

**Files:**
- Modify: `src/Checker.Avalonia/MainWindow.axaml`
- Modify: `src/Checker.Avalonia/MainWindow.axaml.cs`

Build the home and results states, the one-row action bar, selected-folder panel, selectable level cards, resizable/scrollable error table, and compact operation menu. Implement open location, details, and copy information.

### Task 4: WinForms approved page

**Files:**
- Modify: `src/Checker.WinForms/MainForm.Designer.cs`
- Modify: `src/Checker.WinForms/MainForm.cs`
- Modify: `src/Checker.WinForms/IssueListAdapter.cs`

Mirror the Avalonia workflow and wording using native Windows controls, including both-axis scrolling and resizable columns.

### Task 5: Documentation, verification, and release

**Files:**
- Modify: `发布版/使用说明.txt`
- Regenerate: `发布版/IDC日志检查工具_Avalonia.exe`
- Regenerate: `发布版/IDC日志检查工具_WinForms.exe`
- Regenerate: `发布版/SHA256.txt`

Run targeted tests, the full test suite, Release builds, macOS Avalonia smoke checks where supported, and self-contained win-x64 publishing. Verify filenames, file types, sizes, and SHA-256 values without modifying user test fixtures.
