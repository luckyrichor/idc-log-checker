using IDCLogChecker.Core.Presentation;

namespace IDCLogChecker.Avalonia;

public sealed class FolderResultViewModel(FolderResultPresentation presentation)
{
    public FolderResultPresentation Presentation { get; } = presentation;
    public string Path => Presentation.Path;
    public string FolderName => Presentation.FolderName;
    public string StatusText => Presentation.StatusText;
    public string StatusColor => Presentation.StatusColor;
    public string CountText => $"错误 {Presentation.ErrorCount} · 提示 {Presentation.WarningCount}";
}
