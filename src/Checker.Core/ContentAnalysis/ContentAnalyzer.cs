using IDCLogChecker.Core.Scanning;

namespace IDCLogChecker.Core.ContentAnalysis;

public sealed class ContentAnalyzer
{
    public async Task<ContentAnalysisResult> AnalyzeAsync(
        string deviceName,
        string fileName,
        string path,
        CancellationToken cancellationToken = default)
    {
        var document = await CommandOutputReader.ReadAsync(path, cancellationToken).ConfigureAwait(false);
        var output = CommandOutputNormalizer.Normalize(document, deviceName, fileName);
        var family = DeviceFamilyResolver.Resolve(deviceName, output);
        var kind = CommandClassifier.Classify(fileName);
        var context = new ContentAnalysisContext(deviceName, fileName, path, family, kind, document, output);
        var hasVisibleContent = document.AnalysisLines.Any(line => !string.IsNullOrWhiteSpace(line.TrimStart('\uFEFF')));
        var preview = SensitiveTextRedactor.Redact(
            document.AnalysisLines.FirstOrDefault(line => !string.IsNullOrWhiteSpace(line.TrimStart('\uFEFF'))) ?? string.Empty);

        var executionFindings = GenericExecutionRuleSet.Evaluate(context);
        if (executionFindings.Count > 0)
        {
            return WithFacts(new ContentAnalysisResult(false, kind != CommandKind.Unknown, executionFindings));
        }

        var validation = CommandValidationRegistry.Validate(context);
        if (!validation.IsRecognized)
        {
            return WithFacts(new ContentAnalysisResult(false, false, []));
        }

        if (!validation.IsSuccessful)
        {
            var noOutput = output.EffectiveLines.Count == 0;
            return WithFacts(new ContentAnalysisResult(false, true,
            [
                new ContentFinding(
                    noOutput ? "COMMAND_NO_EFFECTIVE_OUTPUT" : "COMMAND_OUTPUT_UNRECOGNIZED",
                    IssueSeverity.Indeterminate,
                    noOutput ? IssueCode.CommandNoEffectiveOutput : IssueCode.CommandOutputUnrecognized,
                    noOutput ? "命令没有可用于判断的有效正文。" : "命令输出格式与当前规则不一致，程序无法确认执行是否正常。",
                    validation.ExpectedDescription,
                    SensitiveTextRedactor.Redact(output.SafePreview),
                    "人工查看该TXT；如确认是新的正常格式，请提供样本以补充规则。")
            ]));
        }

        var semanticFindings = EvaluateSemantic(kind, output);
        return WithFacts(new ContentAnalysisResult(semanticFindings.Count == 0, true, semanticFindings));

        ContentAnalysisResult WithFacts(ContentAnalysisResult result) => result with
        {
            ByteLength = document.ByteLength,
            RawLineCount = document.RawLineCount,
            HasVisibleContent = hasVisibleContent,
            Preview = preview,
        };
    }

    private static IReadOnlyList<ContentFinding> EvaluateSemantic(
        CommandKind kind,
        NormalizedCommandOutput output)
    {
        var text = string.Join(Environment.NewLine, output.EffectiveLines);
        return kind switch
        {
            CommandKind.Cpu => SystemStatusRules.EvaluateCpu(text),
            CommandKind.Memory => SystemStatusRules.EvaluateMemory(text),
            CommandKind.NtpStatus => SystemStatusRules.EvaluateNtp(text),
            CommandKind.AlarmActive => AlarmStatusRules.Evaluate(text),
            CommandKind.BgpSummary => RoutingStatusRules.EvaluateBgp(text),
            CommandKind.OspfPeer => RoutingStatusRules.EvaluateOspf(text),
            CommandKind.BfdNeighbor => RoutingStatusRules.EvaluateBfd(text),
            CommandKind.Configuration => HardwareStatusRules.EvaluateConfiguration(text),
            CommandKind.Fan or CommandKind.Power or CommandKind.Temperature or
                CommandKind.Optics or CommandKind.Interface or CommandKind.Storage =>
                HardwareStatusRules.Evaluate(kind, text),
            _ => [],
        };
    }
}
