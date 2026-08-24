using IDCLogChecker.Core.Presentation;

namespace IDCLogChecker.WinForms;

public sealed record IssueListRow(
    string SeverityText,
    string CategoryText,
    string DeviceName,
    string FileName,
    string Path,
    string Message,
    string Actual,
    string DetailText,
    string ColorHex);

public static class IssueListAdapter
{
    public static IReadOnlyList<IssueListRow> BuildRows(
        ResultPresentation presentation,
        IssueFilter filter)
    {
        ArgumentNullException.ThrowIfNull(presentation);

        return presentation.Filter(filter).Select(row => new IssueListRow(
            row.SeverityText,
            row.CategoryText,
            row.DeviceName,
            row.FileName,
            row.Path,
            row.Message,
            row.Actual,
            row.DetailText,
            row.Severity switch
            {
                Core.Scanning.IssueSeverity.Error => "#C0392B",
                Core.Scanning.IssueSeverity.Indeterminate => "#7D5BA6",
                _ => "#D78C12",
            })).ToArray();
    }
}
