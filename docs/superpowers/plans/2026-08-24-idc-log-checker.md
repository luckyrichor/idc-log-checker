# IDC Log Checker Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build two portable Windows 11 x64 single-file GUI executables that validate a selected log directory against the embedded 62-device/3,660-file baseline and explain every error or warning in Chinese.

**Architecture:** A UI-independent `Checker.Core` owns the embedded manifest, exact-name comparison, content probes, progress events, and report generation. `Checker.Avalonia` and `Checker.WinForms` are thin front ends over the same async scan API, while `Checker.Tests` generates isolated fixtures under `test/.generated` and verifies every rule.

**Tech Stack:** .NET 10 SDK installed locally, C# 13, System.Text.Json, Avalonia 12, WinForms, xUnit, Microsoft.NET.Test.Sdk.

**Spec:** `docs/superpowers/specs/2026-08-24-idc-log-checker-design.md`

## Global Constraints

- Project root is `/Users/lc/Desktop/work/62监控/IDC日志检查工具_Win11/`.
- All SDKs, NuGet packages, caches, and temporary files stay below `.tools/`.
- Do not use Homebrew, global PATH changes, `/usr/local`, `/opt/homebrew`, or system Applications.
- Windows outputs are self-contained, single-file, `win-x64` executables with the manifest embedded.
- Both front ends consume the same `Checker.Core` API and must never duplicate checking rules.
- Source log folders are read-only inputs; tests create their own fixtures.
- Folder and file baseline comparisons use ordinal, case-sensitive equality even on Windows.
- Errors fail the scan; warnings do not fail an otherwise correct structure.

---

### Task 1: Local toolchain and solution skeleton

**Files:**
- Create: `.gitignore`
- Create: `global.json`
- Create: `Directory.Build.props`
- Create: `scripts/env.sh`
- Create: `scripts/install-dotnet.sh`
- Create: `IDCLogChecker.sln`
- Create: `src/Checker.Core/Checker.Core.csproj`
- Create: `src/Checker.Avalonia/Checker.Avalonia.csproj`
- Create: `src/Checker.WinForms/Checker.WinForms.csproj`
- Create: `tests/Checker.Tests/Checker.Tests.csproj`

**Interfaces:**
- Produces: local `dotnet` command through `scripts/env.sh`; buildable solution with project references.

- [ ] Write scripts that export `DOTNET_ROOT`, `DOTNET_CLI_HOME`, `NUGET_PACKAGES`, `NUGET_HTTP_CACHE_PATH`, and `TMPDIR` to project-local directories.
- [ ] Download Microsoft's official `dotnet-install.sh` to `.tools/downloads/` and install the pinned .NET 10 SDK into `.tools/dotnet/`.
- [ ] Verify `.tools/dotnet/dotnet --info` reports the pinned SDK and arm64 macOS.
- [ ] Create the four projects and solution references; set `EnableWindowsTargeting=true` only on WinForms.
- [ ] Run `dotnet restore` with project-local caches and verify no package cache is created under the user's global NuGet directory during this run.
- [ ] Commit the skeleton.

### Task 2: Reproducible embedded manifest

**Files:**
- Create: `scripts/generate-manifest.py`
- Create: `src/Checker.Core/Baseline/manifest.json`
- Create: `src/Checker.Core/Baseline/BaselineManifest.cs`
- Test: `tests/Checker.Tests/BaselineManifestTests.cs`

**Interfaces:**
- Produces: `BaselineManifest.LoadEmbedded(): BaselineManifest`; `IReadOnlyDictionary<string, IReadOnlySet<string>> Devices`.

- [ ] Write a failing test asserting 62 devices, 3,660 total TXT names, no duplicate names, and expected device/file spot checks.
- [ ] Run the targeted test and confirm it fails because the manifest loader does not exist.
- [ ] Implement a deterministic generator that reads only immediate device directories and immediate `.txt` files, sorts names ordinally, and writes schema version, source names, SHA-256, and devices.
- [ ] Generate from `../LogRst_20260823_2359`; independently compare every generated name against `../LogRst_20260801_0004` before accepting output.
- [ ] Embed `manifest.json` in `Checker.Core` and implement `LoadEmbedded()` with duplicate/schema validation.
- [ ] Run the targeted test and verify it passes.
- [ ] Commit the manifest and loader.

### Task 3: Exact structure comparison

**Files:**
- Create: `src/Checker.Core/Scanning/IssueSeverity.cs`
- Create: `src/Checker.Core/Scanning/IssueCode.cs`
- Create: `src/Checker.Core/Scanning/ScanIssue.cs`
- Create: `src/Checker.Core/Scanning/ScanSummary.cs`
- Create: `src/Checker.Core/Scanning/ScanResult.cs`
- Create: `src/Checker.Core/Scanning/DirectoryScanner.cs`
- Test: `tests/Checker.Tests/DirectoryStructureTests.cs`
- Test: `tests/Checker.Tests/FileStructureTests.cs`

**Interfaces:**
- Produces: `Task<ScanResult> DirectoryScanner.ScanAsync(string rootPath, IProgress<ScanProgress>? progress, CancellationToken cancellationToken)`.

- [ ] Write failing tests for valid structure, missing/extra directories, case-only mismatch, missing/extra TXT, case-only TXT mismatch, non-TXT files, and nested directories.
- [ ] Run the two test classes and confirm failures identify the missing scanner.
- [ ] Implement ordinal set comparison and case-insensitive pairing used only to produce clearer case-mismatch messages.
- [ ] Implement root validation and immediate-child enumeration with per-path exception capture.
- [ ] Return ordered issues with expected and actual names and user-readable Chinese messages.
- [ ] Run the two test classes and verify all structure cases pass.
- [ ] Commit structure scanning.

### Task 4: Empty and one-line content checks

**Files:**
- Create: `src/Checker.Core/Scanning/TextContentProbe.cs`
- Test: `tests/Checker.Tests/TextContentProbeTests.cs`

**Interfaces:**
- Produces: `Task<TextProbeResult> TextContentProbe.ProbeAsync(string path, CancellationToken cancellationToken)` returning `Empty`, `OneLine`, `MultipleLines`, `Unreadable`, line preview, byte length, and exception text.

- [ ] Write failing tests for zero bytes, UTF-8 BOM only, whitespace-only content, one line with/without newline, CRLF, two lines, very large files, invalid UTF-8 fallback, and unreadable input.
- [ ] Run the targeted tests and verify failure.
- [ ] Implement a bounded probe that reads only enough characters/lines to classify empty, one-line, or multiple-line content; cap preview at 200 characters.
- [ ] Integrate probe results into `DirectoryScanner`: empty/unreadable are errors; one line is a warning.
- [ ] Run content tests and all previous scanner tests.
- [ ] Commit content checks.

### Task 5: Human-readable report generation

**Files:**
- Create: `src/Checker.Core/Reporting/ChineseTextReportWriter.cs`
- Test: `tests/Checker.Tests/ChineseTextReportWriterTests.cs`

**Interfaces:**
- Produces: `string ChineseTextReportWriter.Write(ScanResult result)` and `Task SaveAsync(ScanResult result, string path, CancellationToken token)`.

- [ ] Write failing snapshot-style tests for a clean result, warnings-only result, and mixed errors/warnings result.
- [ ] Run targeted tests and verify failure.
- [ ] Implement UTF-8 report sections for selected path, scan time, conclusion, counts, errors, warnings, and actionable details.
- [ ] Ensure report output never exposes file contents beyond the bounded one-line preview.
- [ ] Run report tests and full core tests.
- [ ] Commit reporting.

### Task 6: Avalonia front end and Mac smoke test

**Files:**
- Create: `src/Checker.Avalonia/Program.cs`
- Create: `src/Checker.Avalonia/App.axaml`
- Create: `src/Checker.Avalonia/App.axaml.cs`
- Create: `src/Checker.Avalonia/MainWindow.axaml`
- Create: `src/Checker.Avalonia/MainWindow.axaml.cs`
- Create: `src/Checker.Avalonia/MainWindowViewModel.cs`
- Create: `src/Checker.Avalonia/IssueRowViewModel.cs`
- Create: `src/Checker.Avalonia/Assets/app-icon.ico`
- Test: `tests/Checker.Tests/AvaloniaViewModelTests.cs`

**Interfaces:**
- Consumes: `DirectoryScanner.ScanAsync`, `ChineseTextReportWriter.SaveAsync`.
- Produces: macOS arm64 development app and Windows x64 publishable Avalonia executable.

- [ ] Write failing view-model tests for initial state, folder selection state, running state, clean/warning/error summary text, filters, and export availability.
- [ ] Run tests and verify failure.
- [ ] Implement the view model with `INotifyPropertyChanged`, async commands, progress, cancellation, filters, and error dialogs.
- [ ] Build the Chinese XAML layout with large controls, high-contrast status cards, sortable details, and accessible labels.
- [ ] Run tests and render/run the macOS arm64 build against generated fixtures and both real log directories.
- [ ] Capture Mac smoke-test observations in `test/results/avalonia-mac-smoke-test.txt`.
- [ ] Commit Avalonia UI.

### Task 7: WinForms front end

**Files:**
- Create: `src/Checker.WinForms/Program.cs`
- Create: `src/Checker.WinForms/MainForm.cs`
- Create: `src/Checker.WinForms/MainForm.Designer.cs`
- Create: `src/Checker.WinForms/IssueListAdapter.cs`
- Create: `src/Checker.WinForms/Properties/ApplicationIcon.ico`
- Test: `tests/Checker.Tests/IssueListAdapterTests.cs`

**Interfaces:**
- Consumes: the same core scanner and report writer.
- Produces: Windows-only native front end with matching visible labels and result semantics.

- [ ] Write failing adapter tests verifying row text, severity color, filter behavior, and detail text match the shared result.
- [ ] Run tests and verify failure.
- [ ] Implement WinForms layout and async event handlers without duplicating scan rules.
- [ ] Add folder selection, progress, filters, report export, copy, and open-location actions.
- [ ] Cross-build `win-x64` and inspect PE architecture and embedded manifest resources.
- [ ] Record the unrun-on-Mac limitation in `test/results/winforms-cross-build.txt`.
- [ ] Commit WinForms UI.

### Task 8: Test fixtures and real-data verification

**Files:**
- Create: `test/README.txt`
- Create: `test/automated/run-all-tests.sh`
- Create: `test/automated/generate-cases.sh`
- Create: `test/cases/case-catalog.json`
- Create: `test/results/expected-results.txt`
- Create: `tests/Checker.Tests/EndToEndFixtureTests.cs`

**Interfaces:**
- Produces: reproducible test cases in `test/.generated/` and reports in `test/results/`.

- [ ] Write failing end-to-end tests for all 18 approved scenarios, including wrong root level, Chinese/space/long path, multiple simultaneous findings, and report export.
- [ ] Implement fixture generation from the embedded manifest with tiny two-line valid files, then apply one named mutation per case.
- [ ] Run all automated tests and preserve a concise machine-readable and Chinese text result summary.
- [ ] Scan both real source batches read-only and assert 62 directory names, 3,660 expected files, zero structural errors, and expected one-line warnings.
- [ ] Verify no test writes occurred under either source log folder.
- [ ] Commit tests and results.

### Task 9: Windows single-file publishing and delivery verification

**Files:**
- Create: `scripts/publish-windows.sh`
- Create: `scripts/publish-macos-test.sh`
- Create: `发布版/使用说明.txt`
- Create: `发布版/SHA256.txt`
- Create: `test/results/release-verification.txt`

**Interfaces:**
- Produces: `发布版/IDC日志检查工具_Avalonia.exe` and `发布版/IDC日志检查工具_WinForms.exe`.

- [ ] Publish both projects for `win-x64`, Release, self-contained, single-file, with debug symbols excluded and manifest/resources embedded.
- [ ] Verify each release directory contains exactly one EXE plus the separately delivered Chinese usage and checksum documents outside the executable count.
- [ ] Inspect both files as PE32+ x86-64 executables and compute SHA-256.
- [ ] Run the complete automated suite again from a clean project-local cache state that does not rely on previous build outputs.
- [ ] Verify Avalonia macOS build launches and scans a valid and invalid fixture after final core changes.
- [ ] Write `release-verification.txt` with commands, exit codes, test totals, known Mac/Windows validation boundary, file sizes, and hashes.
- [ ] Commit release scripts and documentation.

### Task 10: Final review and handoff

**Files:**
- Modify: `test/results/release-verification.txt`
- Modify: `发布版/使用说明.txt`

**Interfaces:**
- Consumes: all prior deliverables.
- Produces: auditable handoff ready for Windows 11 user testing.

- [ ] Run placeholder/TODO scans and verify no incomplete user-facing strings remain.
- [ ] Compare embedded manifest hash against a freshly generated manifest from each source batch.
- [ ] Verify both EXEs, test results, source files, and instructions are inside the project root only.
- [ ] Run `dotnet test`, both publish commands, PE inspection, SHA-256 generation, and Mac Avalonia smoke test one final time.
- [ ] Record any checks that cannot be executed without Windows as pending physical-platform validation, never as passed.
- [ ] Commit final verification evidence and present exact Windows test steps to the user.
