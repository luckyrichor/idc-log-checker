namespace IDCLogChecker.Core.ContentAnalysis;

public static class CommandValidationRegistry
{
    public static CommandValidationResult Validate(ContentAnalysisContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var validator = CoreCommandValidators.All.FirstOrDefault(item => item.CanValidate(context));
        return validator?.Validate(context) ?? CommandValidationResult.Unsupported;
    }
}
