using IDCLogChecker.Core.Presentation;

namespace IDCLogChecker.WinForms;

public sealed record IssueListRow(
    string SeverityText,
    string CategoryText,
    string DeviceName,
    string FileName,
    string Path,
    string Message,
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
            row.DetailText,
            row.Severity switch
            {
                Core.Scanning.IssueSeverity.Error => "#C0392B",
                _ => "#D78C12",
            })).ToArray();
    }
}
