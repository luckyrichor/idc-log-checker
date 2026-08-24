using IDCLogChecker.Core.Presentation;

namespace IDCLogChecker.Avalonia;

public sealed class FolderResultViewModel(FolderResultPresentation presentation)
{
    public FolderResultPresentation Presentation { get; } = presentation;
    public string Path => Presentation.Path;
    public string FolderName => Presentation.FolderName;
    public string StatusText => Presentation.StatusText;
    public string StatusColor => Presentation.StatusColor;
    public int ErrorCount => Presentation.ErrorCount;
    public int IndeterminateCount => Presentation.IndeterminateCount;
    public int WarningCount => Presentation.WarningCount;
    public string CountText => $"错误 {ErrorCount} · 无法确认 {IndeterminateCount} · 提示 {WarningCount}";
}
