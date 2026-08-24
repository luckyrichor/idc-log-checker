# TXT Content Validation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add explainable TXT command-output validation, typed device-state findings, indeterminate-result handling, reliable missing-path location opening, and user-resizable result columns to both Windows interfaces.

**Architecture:** Keep `DirectoryScanner` as the scan coordinator and add a focused `ContentAnalysis` module that streams and normalizes each expected TXT once, resolves device/command families, then runs generic execution rules, command success validators, and semantic status rules in that order. Extend the shared presentation/reporting model with an indeterminate severity and grouped details so Avalonia and WinForms consume identical decisions; keep platform-specific code limited to file pickers, Explorer launching, and controls.

**Tech Stack:** C# / .NET 10, xUnit, Avalonia 12.1.1 plus `Avalonia.Controls.DataGrid` 12.1.1, Windows Forms, embedded baseline/rule data, self-contained Windows x64 single-file publishing.

**Spec:** `docs/superpowers/specs/2026-08-24-txt-content-validation-design.md`

## Global Constraints

- Windows target remains Windows 11 x64; both deliverables remain self-contained single EXE files and require no installed .NET or network access.
- Use only project-local SDK, NuGet, cache, and temporary paths configured by `scripts/env.sh`; the DataGrid package is restored into `.tools/nuget-packages`.
- Log folders are read-only inputs. Real-data tests must prove relative path, byte length, and modification time are unchanged.
- Every content finding includes stable rule code, severity, device name, TXT filename, full path, Chinese explanation, expected/actual text, and suggested action.
- Reports must redact passwords, SNMP communities, authentication keys, ciphertext, and full configuration fragments.
- Unknown output must never be auto-learned as normal. Missing a required success signature yields `Indeterminate`, not success.
- Dynamic timestamps, counters, routing entries, and log text are not compared byte-for-byte with a prior run.
- Existing untracked `test/test_cases/` and `test/test_cases_windows.zip` are user-owned test copies and must not be added, edited, deleted, or committed.

---

### Task 1: Extend the shared result domain for indeterminate content findings

**Files:**
- Modify: `src/Checker.Core/Scanning/IssueSeverity.cs`
- Modify: `src/Checker.Core/Scanning/IssueCode.cs`
- Modify: `src/Checker.Core/Scanning/ScanIssue.cs`
- Modify: `src/Checker.Core/Scanning/ScanSummary.cs`
- Modify: `src/Checker.Core/Presentation/ResultPresentation.cs`
- Modify: `src/Checker.Core/Presentation/BatchResultPresentation.cs`
- Test: `tests/Checker.Tests/ResultPresentationTests.cs`
- Test: `tests/Checker.Tests/BatchResultPresentationTests.cs`

**Interfaces:**
- Produces: `IssueSeverity.Indeterminate`, typed content `IssueCode` values, `ScanIssue.RuleCode`, `ScanIssue.SuggestedAction`, `ScanSummary.IndeterminateCount`, `ScanSummary.ContentNormalCount`, and `ScanSummary.UnsupportedContentRuleCount`.
- Produces: `IssueFilter.Indeterminate` and presentation ordering `Error -> Indeterminate -> Warning`.

- [ ] **Step 1: Write failing domain and presentation tests**

```csharp
[Fact]
public void IndeterminateFindingsProduceIncompleteConclusionAndPurpleRows()
{
    var issue = new ScanIssue(
        IssueSeverity.Indeterminate,
        IssueCode.CommandOutputUnrecognized,
        "未找到预期的CPU使用率字段。",
        "Device-A",
        @"C:\logs\Device-A\display cpu.txt",
        Expected: "CPU使用率字段",
        Actual: "未知返回格式")
    { RuleCode = "CPU_OUTPUT_UNRECOGNIZED", SuggestedAction = "人工查看TXT内容。" };
    var result = TestResult([issue], indeterminateCount: 1);

    var presentation = ResultPresentation.From(result);

    Assert.Contains("未完全确认", presentation.Conclusion);
    Assert.Equal("#7D5BA6", presentation.StatusColor);
    Assert.Equal("无法确认", presentation.AllRows.Single().SeverityText);
    Assert.Single(presentation.Filter(IssueFilter.Indeterminate));
}
```

- [ ] **Step 2: Run focused tests and verify the new symbols are missing**

Run: `source scripts/env.sh; dotnet test tests/Checker.Tests/Checker.Tests.csproj --configuration Release --filter 'FullyQualifiedName~ResultPresentationTests|FullyQualifiedName~BatchResultPresentationTests'`

Expected: FAIL because `Indeterminate`, `CommandOutputUnrecognized`, and the new summary properties do not exist.

- [ ] **Step 3: Implement the domain additions without breaking existing constructors**

```csharp
public enum IssueSeverity
{
    Warning = 1,
    Indeterminate = 2,
    Error = 3,
}

public sealed record ScanIssue(/* existing positional parameters */)
{
    public string RuleCode { get; init; } = string.Empty;
    public string SuggestedAction { get; init; } = string.Empty;
}

public sealed record ScanSummary(/* existing positional parameters */)
{
    public int IndeterminateCount { get; init; }
    public int ContentNormalCount { get; init; }
    public int UnsupportedContentRuleCount { get; init; }
}
```

Add explicit `IssueCode` values for generic command failures, no effective output, unrecognized output, NTP, CPU, memory, alarm levels, BGP/OSPF/BFD, interface, fan, power, temperature, optics, storage, and security risk. Update conclusion, colors, filters, default folder selection, and detail text to expose rule code and suggested action.

- [ ] **Step 4: Run focused tests and the existing presentation suite**

Run: `source scripts/env.sh; dotnet test tests/Checker.Tests/Checker.Tests.csproj --configuration Release --filter 'FullyQualifiedName~Presentation'`

Expected: PASS with legacy error/warning behavior unchanged and indeterminate behavior covered.

- [ ] **Step 5: Commit the shared result domain**

```bash
git add src/Checker.Core/Scanning src/Checker.Core/Presentation tests/Checker.Tests/ResultPresentationTests.cs tests/Checker.Tests/BatchResultPresentationTests.cs
git commit -m "feat: model indeterminate content findings"
```

### Task 2: Stream and normalize command output safely

**Files:**
- Create: `src/Checker.Core/ContentAnalysis/CommandOutputDocument.cs`
- Create: `src/Checker.Core/ContentAnalysis/CommandOutputReader.cs`
- Create: `src/Checker.Core/ContentAnalysis/CommandOutputNormalizer.cs`
- Create: `src/Checker.Core/ContentAnalysis/ContentAnalysisContext.cs`
- Create: `src/Checker.Core/ContentAnalysis/DeviceFamily.cs`
- Create: `src/Checker.Core/ContentAnalysis/DeviceFamilyResolver.cs`
- Create: `src/Checker.Core/ContentAnalysis/CommandKind.cs`
- Create: `src/Checker.Core/ContentAnalysis/CommandClassifier.cs`
- Test: `tests/Checker.Tests/CommandOutputReaderTests.cs`
- Test: `tests/Checker.Tests/CommandClassificationTests.cs`

**Interfaces:**
- Produces: `Task<CommandOutputDocument> CommandOutputReader.ReadAsync(string path, CancellationToken)`.
- Produces: `NormalizedCommandOutput CommandOutputNormalizer.Normalize(CommandOutputDocument document, string deviceName, string fileName)`.
- Produces: `DeviceFamily DeviceFamilyResolver.Resolve(string deviceName, NormalizedCommandOutput output)`.
- Produces: `CommandKind CommandClassifier.Classify(string fileName)`.
- Produces: `ContentAnalysisContext` carrying device, file, path, family, command kind, document, and normalized output to every downstream rule.

- [ ] **Step 1: Write failing normalization and classification tests**

```csharp
[Fact]
public async Task RemovesBomPromptCommandEchoPagingAndTrailingPrompt()
{
    var path = TestDirectory.WriteText(
        "display cpu.txt",
        "\uFEFF<Device-A>\r\n<Device-A>display cpu\r\nCPU Usage : 12%\r\n---- More ----\r\n<Device-A>\r\n");

    var document = await CommandOutputReader.ReadAsync(path, default);
    var output = CommandOutputNormalizer.Normalize(document, "Device-A", "display cpu.txt");

    Assert.Equal(["CPU Usage : 12%"], output.EffectiveLines);
    Assert.Equal(5, output.RawLineCount);
}

[Theory]
[InlineData("SH-X-S5552", DeviceFamily.S5552)]
[InlineData("SH-X-CE16808-HX", DeviceFamily.CE16808)]
[InlineData("SH-X-N18010", DeviceFamily.N18010)]
public void ResolvesDeviceFamilyFromDeviceName(string name, DeviceFamily expected) =>
    Assert.Equal(expected, DeviceFamilyResolver.Resolve(name, NormalizedCommandOutput.Empty));
```

- [ ] **Step 2: Run focused tests and verify they fail**

Run: `source scripts/env.sh; dotnet test tests/Checker.Tests/Checker.Tests.csproj --configuration Release --filter 'FullyQualifiedName~CommandOutput|FullyQualifiedName~CommandClassification'`

Expected: FAIL because the `ContentAnalysis` types do not exist.

- [ ] **Step 3: Implement bounded-memory reading and normalization**

```csharp
public sealed record CommandOutputDocument(
    string Path,
    long ByteLength,
    int RawLineCount,
    IReadOnlyList<string> AnalysisLines,
    string Preview,
    bool TruncatedForAnalysis);

public sealed record NormalizedCommandOutput(
    int RawLineCount,
    IReadOnlyList<string> EffectiveLines,
    string SafePreview,
    bool TruncatedForAnalysis)
{
    public static NormalizedCommandOutput Empty { get; } = new(0, [], string.Empty, false);
}

public sealed record ContentAnalysisContext(
    string DeviceName,
    string FileName,
    string Path,
    DeviceFamily DeviceFamily,
    CommandKind CommandKind,
    CommandOutputDocument Document,
    NormalizedCommandOutput Output);
```

Keep at most the first and last bounded analysis windows plus aggregate counters. Downstream validators inspect only those bounded windows and command-specific aggregates, so large route/config files are never retained in full. Treat UTF-8 BOM, CRLF/LF, common prompt forms, command echo, `---- More ----`, and trailing prompts explicitly.

- [ ] **Step 4: Implement device-family and command classification tables**

Map the known device suffixes and the 210 observed filename patterns into broad `CommandKind` values such as CPU, Memory, Clock, NtpStatus, Version, AlarmActive, BgpSummary, BgpAdvertisedRoutes, OspfPeer, BfdNeighbor, Configuration, Interface, Fan, Power, Temperature, Optics, Log, and Unknown.

- [ ] **Step 5: Run focused tests and a large-file memory regression**

Run: `source scripts/env.sh; dotnet test tests/Checker.Tests/Checker.Tests.csproj --configuration Release --filter 'FullyQualifiedName~CommandOutput|FullyQualifiedName~CommandClassification'`

Expected: PASS, including a generated multi-megabyte route output test that confirms bounded retained text.

- [ ] **Step 6: Commit the reader and classifiers**

```bash
git add src/Checker.Core/ContentAnalysis tests/Checker.Tests/CommandOutputReaderTests.cs tests/Checker.Tests/CommandClassificationTests.cs
git commit -m "feat: normalize network command output"
```

### Task 3: Detect generic command execution failures with scoped rules

**Files:**
- Create: `src/Checker.Core/ContentAnalysis/ContentFinding.cs`
- Create: `src/Checker.Core/ContentAnalysis/GenericExecutionRuleSet.cs`
- Create: `src/Checker.Core/ContentAnalysis/SensitiveTextRedactor.cs`
- Test: `tests/Checker.Tests/GenericExecutionRuleSetTests.cs`
- Test: `tests/Checker.Tests/SensitiveTextRedactorTests.cs`

**Interfaces:**
- Produces: `IReadOnlyList<ContentFinding> GenericExecutionRuleSet.Evaluate(ContentAnalysisContext context)`.
- Produces: `string SensitiveTextRedactor.Redact(string text)`.

- [ ] **Step 1: Write one failing theory covering every explicit execution category**

```csharp
[Theory]
[InlineData("% Unrecognized command found at '^' position.", IssueCode.CommandUnrecognized)]
[InlineData("% No such neighbor or address family(BGP Instance AS 1)", IssueCode.BgpNeighborAddressFamilyNotFound)]
[InlineData("Info: The peer does not exist.", IssueCode.BgpPeerNotFound)]
[InlineData("%Info 4031: No such neighbor.", IssueCode.BgpNeighborNotFound)]
[InlineData("Error: Too many parameters found at '^' position.", IssueCode.CommandTooManyParameters)]
[InlineData("% Invalid input detected", IssueCode.CommandInvalidInput)]
[InlineData("% Incomplete command", IssueCode.CommandIncomplete)]
public void ClassifiesExplicitCliFailure(string line, IssueCode code)
{
    var finding = Evaluate(line).Single();
    Assert.Equal(code, finding.Code);
    Assert.Equal(IssueSeverity.Error, finding.Severity);
}
```

Add negative tests proving the words `failed` and `timeout` inside `display logbuffer.txt` do not become current command-execution failures.

- [ ] **Step 2: Run focused tests and verify failure**

Run: `source scripts/env.sh; dotnet test tests/Checker.Tests/Checker.Tests.csproj --configuration Release --filter 'FullyQualifiedName~GenericExecutionRuleSet|FullyQualifiedName~SensitiveTextRedactor'`

Expected: FAIL because the rule set and redactor do not exist.

- [ ] **Step 3: Implement ordered, command-scoped rules and stable codes**

```csharp
public sealed record ContentFinding(
    string RuleCode,
    IssueSeverity Severity,
    IssueCode Code,
    string Message,
    string Expected,
    string Actual,
    string SuggestedAction);
```

Evaluate direct CLI response lines before semantic rules. Use longest/specific patterns before broader patterns so `No such neighbor or address family` is not double-counted as `No such neighbor`. Timeout, permission, and connection rules only match direct response regions and never arbitrary log/config bodies.

- [ ] **Step 4: Implement redaction before any preview enters a finding**

Cover password/cipher/secret/key/community lines case-insensitively and replace the value with `***已隐藏***`. Limit actual excerpts to a fixed character count after redaction.

- [ ] **Step 5: Run the rule and redaction tests**

Run: `source scripts/env.sh; dotnet test tests/Checker.Tests/Checker.Tests.csproj --configuration Release --filter 'FullyQualifiedName~GenericExecutionRuleSet|FullyQualifiedName~SensitiveTextRedactor'`

Expected: PASS with no duplicate neighbor findings and no credentials in test output.

- [ ] **Step 6: Commit generic execution analysis**

```bash
git add src/Checker.Core/ContentAnalysis tests/Checker.Tests/GenericExecutionRuleSetTests.cs tests/Checker.Tests/SensitiveTextRedactorTests.cs
git commit -m "feat: classify command execution failures"
```

### Task 4: Add command success validators and the indeterminate fallback

**Files:**
- Create: `src/Checker.Core/ContentAnalysis/ICommandOutputValidator.cs`
- Create: `src/Checker.Core/ContentAnalysis/CommandValidationRegistry.cs`
- Create: `src/Checker.Core/ContentAnalysis/CoreCommandValidators.cs`
- Create: `src/Checker.Core/ContentAnalysis/ContentAnalyzer.cs`
- Test: `tests/Checker.Tests/CommandValidationRegistryTests.cs`
- Test: `tests/Checker.Tests/ContentAnalyzerTests.cs`

**Interfaces:**
- Produces: `CommandValidationResult ICommandOutputValidator.Validate(ContentAnalysisContext context)`.
- Produces: `Task<ContentAnalysisResult> ContentAnalyzer.AnalyzeAsync(string deviceName, string fileName, string path, CancellationToken)`.

- [ ] **Step 1: Write failing tests for success, allowed-empty, required-body, and unknown format**

```csharp
[Fact]
public async Task UnknownCpuFormatIsIndeterminateInsteadOfSuccess()
{
    var result = await Analyze("Device-S5552", "display cpu.txt", "NEW VENDOR MESSAGE");

    var finding = Assert.Single(result.Findings);
    Assert.Equal(IssueSeverity.Indeterminate, finding.Severity);
    Assert.Equal(IssueCode.CommandOutputUnrecognized, finding.Code);
}

[Fact]
public async Task EmptyDebuggingBodyIsAllowed()
{
    var result = await Analyze("Device-N18010", "show debugging.txt", "Device-N18010#show debugging\r\n");
    Assert.Empty(result.Findings);
    Assert.True(result.IsContentNormal);
}
```

- [ ] **Step 2: Run focused tests and verify failure**

Run: `source scripts/env.sh; dotnet test tests/Checker.Tests/Checker.Tests.csproj --configuration Release --filter 'FullyQualifiedName~CommandValidation|FullyQualifiedName~ContentAnalyzer'`

Expected: FAIL because validators and analyzer do not exist.

- [ ] **Step 3: Implement validator registry and precedence**

```csharp
public sealed record CommandValidationResult(
    bool IsRecognized,
    bool IsSuccessful,
    string ExpectedDescription,
    IReadOnlyDictionary<string, string> ParsedValues);

public sealed record ContentAnalysisResult(
    bool IsContentNormal,
    bool HasDedicatedRule,
    IReadOnlyList<ContentFinding> Findings);
```

Execution errors stop success/semantic parsing for that TXT. Otherwise the registry resolves the most-specific `(DeviceFamily, CommandKind)` validator, then falls back to a family-agnostic validator. A missing validator increments unsupported-rule count but does not create an error; a present validator that cannot confirm success creates one indeterminate finding.

- [ ] **Step 4: Implement first-version success signatures**

Add validators for CPU, memory, clock, NTP, version, OSPF/OSPFv3, BGP summary, BGP advertised routes, active alarms, configuration, debugging, fan, power, temperature, and optics across the supplied device families. Use multiple structural markers where one generic word could appear in unrelated text.

- [ ] **Step 5: Run focused tests and confirm no unknown output is silently normal**

Run: `source scripts/env.sh; dotnet test tests/Checker.Tests/Checker.Tests.csproj --configuration Release --filter 'FullyQualifiedName~CommandValidation|FullyQualifiedName~ContentAnalyzer'`

Expected: PASS.

- [ ] **Step 6: Commit command validation**

```bash
git add src/Checker.Core/ContentAnalysis tests/Checker.Tests/CommandValidationRegistryTests.cs tests/Checker.Tests/ContentAnalyzerTests.cs
git commit -m "feat: validate command success signatures"
```

### Task 5: Parse CPU, memory, NTP, and active alarms

**Files:**
- Create: `src/Checker.Core/ContentAnalysis/SystemStatusRules.cs`
- Create: `src/Checker.Core/ContentAnalysis/AlarmStatusRules.cs`
- Test: `tests/Checker.Tests/SystemStatusRulesTests.cs`
- Test: `tests/Checker.Tests/AlarmStatusRulesTests.cs`

**Interfaces:**
- Consumes: normalized output and parsed values from Task 4.
- Produces: semantic `ContentFinding` records for CPU, memory, NTP, and each active alarm entry.

- [ ] **Step 1: Write failing multi-vendor system-status tests**

```csharp
[Theory]
[InlineData("System CPU Using Percentage : 72%", 72)]
[InlineData("CPU Usage : 72% Max: 90%", 72)]
[InlineData("72% in last 5 seconds", 72)]
[InlineData("CPU utilization in five seconds: 72.0%", 72)]
public void CpuAtSeventyPercentCreatesWarning(string output, double expected)
{
    var finding = SystemStatusRules.EvaluateCpu(output).Single();
    Assert.Equal(IssueCode.CpuUsageHigh, finding.Code);
    Assert.Contains(expected.ToString("0"), finding.Actual);
}
```

Add tests for 69% normal, 70% warning, 90% high-priority warning, NTP synchronized/unsynchronized/stratum 16, and memory formats using `Memory Using Percentage`, `used rate`, `FreeRatio`, and 9908X processor tables.

- [ ] **Step 2: Write failing alarm tests for both supplied table formats**

Cover whitespace-table CE output and slash-separated HW930x output. Assert one finding per active alarm, correct Critical/Major/Minor/Warning code, and retained device/file association.

- [ ] **Step 3: Run focused tests and verify failure**

Run: `source scripts/env.sh; dotnet test tests/Checker.Tests/Checker.Tests.csproj --configuration Release --filter 'FullyQualifiedName~SystemStatusRules|FullyQualifiedName~AlarmStatusRules'`

Expected: FAIL because semantic rules do not exist.

- [ ] **Step 4: Implement current-value parsing and alarm-entry parsing**

Do not use historical maximum CPU as current CPU. Keep alarm descriptions bounded and redacted. Preserve the vendor alarm severity as the issue category while mapping all operational-state findings to application `Warning` severity per the approved design.

- [ ] **Step 5: Run system and alarm tests**

Run: `source scripts/env.sh; dotnet test tests/Checker.Tests/Checker.Tests.csproj --configuration Release --filter 'FullyQualifiedName~SystemStatusRules|FullyQualifiedName~AlarmStatusRules'`

Expected: PASS.

- [ ] **Step 6: Commit system status analysis**

```bash
git add src/Checker.Core/ContentAnalysis tests/Checker.Tests/SystemStatusRulesTests.cs tests/Checker.Tests/AlarmStatusRulesTests.cs
git commit -m "feat: analyze system status and alarms"
```

### Task 6: Parse routing-protocol and hardware status

**Files:**
- Create: `src/Checker.Core/ContentAnalysis/RoutingStatusRules.cs`
- Create: `src/Checker.Core/ContentAnalysis/HardwareStatusRules.cs`
- Test: `tests/Checker.Tests/RoutingStatusRulesTests.cs`
- Test: `tests/Checker.Tests/HardwareStatusRulesTests.cs`

**Interfaces:**
- Produces: `BgpNotEstablished`, `OspfNotFull`, `BfdDown`, `InterfaceDown`, `FanAbnormal`, `PowerAbnormal`, `TemperatureHigh`, `OpticalAbnormal`, `StorageUsageHigh`, and `SecurityRisk` findings.

- [ ] **Step 1: Write failing routing-state tests**

```csharp
[Theory]
[InlineData("117.1.1.1 4 65000 0 0 0 00:01:00 Active 0", "Active")]
[InlineData("117.1.1.1 4 65000 0 0 0 never Idle(Admin)", "Idle(Admin)")]
[InlineData("All peers : 3\n  Connect : 1", "Connect")]
public void NonEstablishedBgpStateIsAWarning(string output, string state)
{
    var finding = RoutingStatusRules.EvaluateBgp(output).Single();
    Assert.Equal(IssueCode.BgpNotEstablished, finding.Code);
    Assert.Contains(state, finding.Actual);
}
```

Add OSPF Full/non-Full and BFD Up/Down tests for each observed vendor layout. Verify `Established`, `Full`, and `Up` do not produce warnings.

- [ ] **Step 2: Write failing hardware-state tests**

Cover explicit status words and threshold fields for fan, power, temperature, optics, storage, and security risk. Interface Down remains a warning and retains `AdminStatus`/`OperStatus` text rather than claiming a fault.

- [ ] **Step 3: Run focused tests and verify failure**

Run: `source scripts/env.sh; dotnet test tests/Checker.Tests/Checker.Tests.csproj --configuration Release --filter 'FullyQualifiedName~RoutingStatusRules|FullyQualifiedName~HardwareStatusRules'`

Expected: FAIL because routing/hardware rules do not exist.

- [ ] **Step 4: Implement conservative parsers**

Only emit a semantic warning when the command validator has already confirmed the correct output structure. Include state, peer/interface identifier, and suggested manual check. Do not turn no-neighbor, Idle(Admin), or administratively down interfaces into application errors without a topology baseline.

- [ ] **Step 5: Run routing and hardware tests**

Run: `source scripts/env.sh; dotnet test tests/Checker.Tests/Checker.Tests.csproj --configuration Release --filter 'FullyQualifiedName~RoutingStatusRules|FullyQualifiedName~HardwareStatusRules'`

Expected: PASS.

- [ ] **Step 6: Commit routing and hardware analysis**

```bash
git add src/Checker.Core/ContentAnalysis tests/Checker.Tests/RoutingStatusRulesTests.cs tests/Checker.Tests/HardwareStatusRulesTests.cs
git commit -m "feat: analyze routing and hardware status"
```

### Task 7: Integrate content analysis into directory scanning and prove real-data counts

**Files:**
- Modify: `src/Checker.Core/Scanning/DirectoryScanner.cs`
- Modify: `src/Checker.Core/Scanning/TextContentProbe.cs`
- Modify: `tests/Checker.Tests/FixtureCaseFactory.cs`
- Modify: `tests/Checker.Tests/EndToEndFixtureTests.cs`
- Modify: `tests/Checker.Tests/RealDataVerificationTests.cs`
- Create: `tests/Checker.Tests/RealContentAnalysisTests.cs`

**Interfaces:**
- Consumes: `ContentAnalyzer.AnalyzeAsync` from Task 4.
- Produces: content findings converted to `ScanIssue`, populated content counters, and progress that continues after per-file analysis failures.

- [ ] **Step 1: Write failing integration fixtures**

Add generated cases for explicit CLI failure, allowed-empty debugging, unknown CPU format, NTP unsynchronized, high CPU, and active alarm. Assert the exact severity/code/device/file/rule/action fields and folder conclusion.

- [ ] **Step 2: Run integration tests and verify failure**

Run: `source scripts/env.sh; dotnet test tests/Checker.Tests/Checker.Tests.csproj --configuration Release --filter 'FullyQualifiedName~EndToEndFixtureTests|FullyQualifiedName~RealContentAnalysisTests'`

Expected: FAIL because `DirectoryScanner` does not call content analysis.

- [ ] **Step 3: Integrate one-pass analysis and remove duplicate full-file probing**

Create the default analyzer once per scanner, analyze only exact expected TXT files, convert each `ContentFinding` to `ScanIssue`, and fill `ContentNormalCount`, `IndeterminateCount`, and `UnsupportedContentRuleCount`. Preserve existing empty/unreadable behavior and continue scanning after a single parser exception.

- [ ] **Step 4: Add read-only real-data regression assertions**

For `LogRst_20260823_2359`, assert at minimum the stable explicit execution counts 270/264/132/4/2, two NTP-unsynchronized devices, and parsed alarm split 16/96/4/31. Assert every finding has device name and TXT filename. For both real folders, snapshot relative path, length, and last-write timestamp before and after.

- [ ] **Step 5: Run all core tests**

Run: `test/automated/run-all-tests.sh`

Expected: PASS with all legacy structural cases and new semantic cases.

- [ ] **Step 6: Commit scanner integration**

```bash
git add src/Checker.Core/Scanning tests/Checker.Tests
git commit -m "feat: integrate TXT content validation"
```

### Task 8: Group reports by severity, type, device, and TXT file with redaction

**Files:**
- Modify: `src/Checker.Core/Reporting/ChineseTextReportWriter.cs`
- Modify: `src/Checker.Core/Reporting/ChineseBatchReportWriter.cs`
- Modify: `tests/Checker.Tests/ChineseTextReportWriterTests.cs`
- Modify: `tests/Checker.Tests/ChineseBatchReportWriterTests.cs`

**Interfaces:**
- Consumes: enriched `ScanIssue` and summary properties.
- Produces: grouped UTF-8 Chinese reports with no credential leakage.

- [ ] **Step 1: Write failing grouping and redaction tests**

```csharp
[Fact]
public void GroupsContentProblemsAndListsEveryDeviceAndTxtFile()
{
    var report = ChineseTextReportWriter.Write(ResultWithTwoSameTypeFindings());

    Assert.Contains("设备不识别命令（2）", report);
    Assert.Contains("设备：Device-A", report);
    Assert.Contains("文件：display bgp peer.txt", report);
    Assert.Contains("设备：Device-B", report);
}

[Fact]
public void ExportNeverContainsCredentialValue()
{
    var report = ChineseTextReportWriter.Write(ResultWithActual("snmp-agent community read SecretValue"));
    Assert.DoesNotContain("SecretValue", report);
    Assert.Contains("***已隐藏***", report);
}
```

- [ ] **Step 2: Run report tests and verify failure**

Run: `source scripts/env.sh; dotnet test tests/Checker.Tests/Checker.Tests.csproj --configuration Release --filter 'FullyQualifiedName~ChineseTextReportWriterTests|FullyQualifiedName~ChineseBatchReportWriterTests'`

Expected: FAIL because reports are flat and do not expose indeterminate/content summaries.

- [ ] **Step 3: Implement deterministic grouped report sections**

Write sections in `Error -> Indeterminate -> Warning` order, then category text, device name, and TXT filename using ordinal ordering. Include rule code, explanation, expected, redacted actual, suggested action, and path. Add content counters to each folder summary and batch report.

- [ ] **Step 4: Run report tests**

Run: `source scripts/env.sh; dotnet test tests/Checker.Tests/Checker.Tests.csproj --configuration Release --filter 'FullyQualifiedName~ReportWriterTests'`

Expected: PASS.

- [ ] **Step 5: Commit grouped reporting**

```bash
git add src/Checker.Core/Reporting tests/Checker.Tests/ChineseTextReportWriterTests.cs tests/Checker.Tests/ChineseBatchReportWriterTests.cs
git commit -m "feat: export grouped content findings"
```

### Task 9: Resolve the nearest existing location for missing paths

**Files:**
- Create: `src/Checker.Core/Presentation/OpenLocationResolver.cs`
- Modify: `src/Checker.Avalonia/MainWindow.axaml.cs`
- Modify: `src/Checker.WinForms/MainForm.cs`
- Test: `tests/Checker.Tests/OpenLocationResolverTests.cs`

**Interfaces:**
- Produces: `OpenLocationTarget? OpenLocationResolver.Resolve(string? issuePath)` with `Path` and `SelectFile`.

- [ ] **Step 1: Write failing resolver tests**

```csharp
[Fact]
public void MissingTxtOpensExistingDeviceDirectory()
{
    using var fixture = TestDirectory.Create();
    var device = Directory.CreateDirectory(Path.Combine(fixture.Path, "Device-A")).FullName;

    var target = OpenLocationResolver.Resolve(Path.Combine(device, "missing.txt"));

    Assert.Equal(device, target!.Path);
    Assert.False(target.SelectFile);
}

[Fact]
public void ExistingFileIsSelected()
{
    using var fixture = TestDirectory.Create();
    var file = fixture.WriteText("present.txt", "ok");
    Assert.True(OpenLocationResolver.Resolve(file)!.SelectFile);
}
```

Add missing-device, existing-directory, empty-path, and no-existing-ancestor cases.

- [ ] **Step 2: Run resolver tests and verify failure**

Run: `source scripts/env.sh; dotnet test tests/Checker.Tests/Checker.Tests.csproj --configuration Release --filter FullyQualifiedName~OpenLocationResolverTests`

Expected: FAIL because the resolver does not exist.

- [ ] **Step 3: Implement nearest-existing-ancestor resolution**

```csharp
public sealed record OpenLocationTarget(string Path, bool SelectFile);

public static OpenLocationTarget? Resolve(string? issuePath)
{
    if (string.IsNullOrWhiteSpace(issuePath)) return null;
    if (File.Exists(issuePath)) return new(issuePath, true);
    if (Directory.Exists(issuePath)) return new(issuePath, false);
    for (var current = Path.GetDirectoryName(issuePath); !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
        if (Directory.Exists(current)) return new(current, false);
    return null;
}
```

- [ ] **Step 4: Update both UI handlers to use the shared target**

Avalonia/macOS uses `open -R` only for an existing file and `open <directory>` otherwise. Windows uses `explorer.exe /select,<file>` only for an existing file and `explorer.exe <directory>` otherwise. A null target displays a Chinese message.

- [ ] **Step 5: Run resolver and UI compile tests**

Run: `source scripts/env.sh; dotnet test tests/Checker.Tests/Checker.Tests.csproj --configuration Release --filter FullyQualifiedName~OpenLocationResolverTests; dotnet build IDCLogChecker.sln --configuration Release --no-restore`

Expected: PASS and build with zero errors.

- [ ] **Step 6: Commit location-opening repair**

```bash
git add src/Checker.Core/Presentation/OpenLocationResolver.cs src/Checker.Avalonia/MainWindow.axaml.cs src/Checker.WinForms/MainForm.cs tests/Checker.Tests/OpenLocationResolverTests.cs
git commit -m "fix: open parent location for missing paths"
```

### Task 10: Present content counters, filters, resizable columns, and full values in Avalonia

**Files:**
- Modify: `src/Checker.Avalonia/Checker.Avalonia.csproj`
- Modify: `src/Checker.Avalonia/MainWindowViewModel.cs`
- Modify: `src/Checker.Avalonia/FolderResultViewModel.cs`
- Modify: `src/Checker.Avalonia/MainWindow.axaml`
- Modify: `src/Checker.Avalonia/MainWindow.axaml.cs`
- Modify: `tests/Checker.Tests/AvaloniaViewModelTests.cs`
- Modify: `tests/Checker.Tests/AvaloniaBatchViewModelTests.cs`

**Interfaces:**
- Consumes: shared content counters, `IssueFilter.Indeterminate`, and enriched `IssueRow`.
- Produces: Avalonia master-detail UI with resizable DataGrid columns, horizontal scrolling, tooltips/full detail, and indeterminate filter.

- [ ] **Step 1: Write failing ViewModel tests for indeterminate counters and filtering**

```csharp
[Fact]
public async Task ShowsIndeterminateCountAndFilter()
{
    var viewModel = ViewModelWithIndeterminateResult();
    await viewModel.RunBatchScanAsync();

    Assert.Equal("1", viewModel.IndeterminateCountText);
    viewModel.ApplyFilter(IssueFilter.Indeterminate);
    Assert.All(viewModel.VisibleIssues, row => Assert.Equal(IssueSeverity.Indeterminate, row.Severity));
}
```

- [ ] **Step 2: Run Avalonia ViewModel tests and verify failure**

Run: `source scripts/env.sh; dotnet test tests/Checker.Tests/Checker.Tests.csproj --configuration Release --filter 'FullyQualifiedName~AvaloniaViewModelTests|FullyQualifiedName~AvaloniaBatchViewModelTests'`

Expected: FAIL because indeterminate properties do not exist.

- [ ] **Step 3: Add the project-local DataGrid package and ViewModel properties**

Add `<PackageReference Include="Avalonia.Controls.DataGrid" Version="12.1.1" />`, include its Fluent style, then add content-normal, content-error, indeterminate, status-warning, and unsupported-rule text properties. Add `OnShowIndeterminateClick`.

- [ ] **Step 4: Replace the fixed ListBox columns with a resizable DataGrid**

Set `CanUserResizeColumns="True"`, `HorizontalScrollBarVisibility="Auto"`, explicit minimum widths, non-star widths for long columns, row selection, text trimming, and per-cell tooltip bindings. Keep the bottom full-detail area selectable and vertically scrollable. Ensure folder result columns are also user-resizable or use a splitter-backed layout.

- [ ] **Step 5: Run ViewModel tests and build the Avalonia project**

Run: `source scripts/env.sh; dotnet test tests/Checker.Tests/Checker.Tests.csproj --configuration Release --filter 'FullyQualifiedName~Avalonia'; dotnet build src/Checker.Avalonia/Checker.Avalonia.csproj --configuration Release`

Expected: PASS with zero build warnings/errors.

- [ ] **Step 6: Commit Avalonia content presentation**

```bash
git add src/Checker.Avalonia tests/Checker.Tests/AvaloniaViewModelTests.cs tests/Checker.Tests/AvaloniaBatchViewModelTests.cs
git commit -m "feat: present content findings in Avalonia"
```

### Task 11: Present content counters, filters, resizable columns, and full values in WinForms

**Files:**
- Modify: `src/Checker.WinForms/IssueListAdapter.cs`
- Modify: `src/Checker.WinForms/BatchFormController.cs`
- Modify: `src/Checker.WinForms/MainForm.Designer.cs`
- Modify: `src/Checker.WinForms/MainForm.cs`
- Modify: `tests/Checker.Tests/IssueListAdapterTests.cs`
- Modify: `tests/Checker.Tests/BatchFormControllerTests.cs`

**Interfaces:**
- Consumes: shared content counters, filters, and enriched rows.
- Produces: WinForms presentation matching Avalonia decisions with user-resizable DataGridView columns and horizontal scrollbars.

- [ ] **Step 1: Write failing adapter/controller tests**

```csharp
[Fact]
public void AdapterUsesPurpleForIndeterminateAndRetainsSuggestion()
{
    var row = IssueListAdapter.BuildRows(IndeterminatePresentation(), IssueFilter.Indeterminate).Single();
    Assert.Equal("#7D5BA6", row.ColorHex);
    Assert.Contains("建议：", row.DetailText);
}
```

- [ ] **Step 2: Run WinForms controller tests and verify failure**

Run: `source scripts/env.sh; dotnet test tests/Checker.Tests/Checker.Tests.csproj --configuration Release --filter 'FullyQualifiedName~IssueListAdapterTests|FullyQualifiedName~BatchFormControllerTests'`

Expected: FAIL because indeterminate presentation is not supported.

- [ ] **Step 3: Implement counters and indeterminate filtering**

Expose the current folder's content counters through the controller, add an indeterminate filter button, and color rows according to shared severity.

- [ ] **Step 4: Make all long-value columns manually resizable**

Use `AutoSizeColumnsMode = None`, `AllowUserToResizeColumns = true`, explicit column widths/minimum widths, and `ScrollBars = Both` for folder and issue grids. Set cell tooltips to full values and keep the full-detail textbox scrollable/selectable. Do not reset user-adjusted widths when filters or folders change.

- [ ] **Step 5: Run controller tests and cross-build WinForms**

Run: `source scripts/env.sh; dotnet test tests/Checker.Tests/Checker.Tests.csproj --configuration Release --filter 'FullyQualifiedName~IssueListAdapterTests|FullyQualifiedName~BatchFormControllerTests'; dotnet build src/Checker.WinForms/Checker.WinForms.csproj --configuration Release --no-restore`

Expected: PASS and Windows cross-build succeeds with zero errors.

- [ ] **Step 6: Commit WinForms content presentation**

```bash
git add src/Checker.WinForms tests/Checker.Tests/IssueListAdapterTests.cs tests/Checker.Tests/BatchFormControllerTests.cs
git commit -m "feat: present content findings in WinForms"
```

### Task 12: Full verification, macOS smoke test, documentation, and Windows republication

**Files:**
- Modify: `test/README.txt`
- Modify: `test/results/avalonia-mac-smoke-test.txt`
- Modify: `test/results/winforms-cross-build.txt`
- Modify: `test/results/release-verification.txt`
- Modify: `发布版/使用说明.txt`
- Modify: `发布版/SHA256.txt` via `scripts/publish-windows.sh`

**Interfaces:**
- Produces: verified release artifacts and user-facing instructions for content categories, filters, resizable columns, and parent-location behavior.

- [ ] **Step 1: Run the full automated suite and full Release build**

Run: `test/automated/run-all-tests.sh`

Expected: all tests PASS, zero failures.

Run: `source scripts/env.sh; dotnet build IDCLogChecker.sln --configuration Release --no-restore`

Expected: build succeeds with 0 warnings and 0 errors.

- [ ] **Step 2: Prove real logs remain unchanged and record semantic totals**

Run the real-data verification tests and compare the before/after manifests of both log roots. Record exact total tests and semantic finding counts; do not claim unsupported rules are normal.

- [ ] **Step 3: Publish and launch the macOS Avalonia smoke-test app**

Run: `scripts/publish-macos-test.sh`

Launch the generated `.app` through LaunchServices. Verify content counters/filter, purple indeterminate presentation, resizable column drag, horizontal scrolling, full detail, and opening the parent for a missing fixture path. Record any action not physically completed as pending rather than passed.

- [ ] **Step 4: Update user and verification documentation**

Document the four result classes, grouped device/file details, rule limitations, sensitive-data handling, resizable columns, missing-path parent opening, and Windows 11 test instructions. Update recorded test totals and platform boundary honestly.

- [ ] **Step 5: Republish both Windows x64 single EXEs**

Run: `scripts/publish-windows.sh`

Expected: `发布版/IDC日志检查工具_Avalonia.exe` and `发布版/IDC日志检查工具_WinForms.exe` are regenerated as PE32+ GUI x86-64 single files and `发布版/SHA256.txt` is updated.

- [ ] **Step 6: Verify final artifacts and repository hygiene**

Run: `file 发布版/IDC日志检查工具_Avalonia.exe 发布版/IDC日志检查工具_WinForms.exe`

Run: `shasum -a 256 发布版/IDC日志检查工具_Avalonia.exe 发布版/IDC日志检查工具_WinForms.exe`

Run: `git diff --check`

Run: `rg -n --hidden -g '!bin/**' -g '!obj/**' -g '!artifacts/**' -e 'TODO|FIXME|PLACEHOLDER|NotImplementedException' src tests`

Expected: both artifacts are Windows x64 GUI executables, hashes match the readable SHA file, no diff whitespace errors, and no implementation placeholders.

- [ ] **Step 7: Commit final documentation and verification records**

```bash
git add test/README.txt test/results 发布版/SHA256.txt 发布版/使用说明.txt
git commit -m "docs: finalize content validation release"
```

- [ ] **Step 8: Run the final post-commit test gate**

Run: `test/automated/run-all-tests.sh; git status --short`

Expected: all tests PASS and status contains only the pre-existing user-owned untracked `test/test_cases/` and `test/test_cases_windows.zip`.
