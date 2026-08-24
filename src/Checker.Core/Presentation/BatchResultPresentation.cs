using IDCLogChecker.Core.Batch;

namespace IDCLogChecker.Core.Presentation;

public sealed record FolderResultPresentation(
    string Path,
    string FolderName,
    string StatusText,
    string StatusColor,
    int ErrorCount,
    int WarningCount,
    ResultPresentation Detail);

public sealed class BatchResultPresentation
{
    private BatchResultPresentation(
        BatchScanResult result,
        IReadOnlyList<FolderResultPresentation> folders,
        int defaultSelectedIndex)
    {
        Result = result;
        Folders = folders;
        DefaultSelectedIndex = defaultSelectedIndex;
    }

    public BatchScanResult Result { get; }
    public IReadOnlyList<FolderResultPresentation> Folders { get; }
    public int DefaultSelectedIndex { get; }

    public static BatchResultPresentation From(BatchScanResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var folders = result.Folders.Select(folder =>
        {
            var detail = ResultPresentation.From(folder.Result);
            var (text, color) = folder switch
            {
                { Failed: true } => ("不通过", "#C0392B"),
                { HasWarnings: true } => ("有提示", "#D78C12"),
                _ => ("完全通过", "#1F8A70"),
            };
            return new FolderResultPresentation(
                folder.Path,
                System.IO.Path.GetFileName(folder.Path),
                text,
                color,
                folder.Result.Summary.ErrorCount,
                folder.Result.Summary.WarningCount,
                detail);
        }).ToArray();

        var selected = Array.FindIndex(folders, folder => folder.ErrorCount > 0);
        if (selected < 0) selected = Array.FindIndex(folders, folder => folder.WarningCount > 0);
        if (selected < 0 && folders.Length > 0) selected = 0;
        return new BatchResultPresentation(result, folders, selected);
    }
}
