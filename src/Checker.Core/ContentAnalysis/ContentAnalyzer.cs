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

        var executionFindings = GenericExecutionRuleSet.Evaluate(context);
        if (executionFindings.Count > 0)
        {
            return new ContentAnalysisResult(false, kind != CommandKind.Unknown, executionFindings);
        }

        var validation = CommandValidationRegistry.Validate(context);
        if (!validation.IsRecognized)
        {
            return new ContentAnalysisResult(false, false, []);
        }

        if (!validation.IsSuccessful)
        {
            var noOutput = output.EffectiveLines.Count == 0;
            return new ContentAnalysisResult(false, true,
            [
                new ContentFinding(
                    noOutput ? "COMMAND_NO_EFFECTIVE_OUTPUT" : "COMMAND_OUTPUT_UNRECOGNIZED",
                    IssueSeverity.Indeterminate,
                    noOutput ? IssueCode.CommandNoEffectiveOutput : IssueCode.CommandOutputUnrecognized,
                    noOutput ? "命令没有可用于判断的有效正文。" : "命令输出格式与当前规则不一致，程序无法确认执行是否正常。",
                    validation.ExpectedDescription,
                    SensitiveTextRedactor.Redact(output.SafePreview),
                    "人工查看该TXT；如确认是新的正常格式，请提供样本以补充规则。")
            ]);
        }

        return new ContentAnalysisResult(true, true, []);
    }
}
