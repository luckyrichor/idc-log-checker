namespace IDCLogChecker.Core.Scanning;

public enum IssueCode
{
    RootNotFound,
    RootUnreadable,
    MissingDirectory,
    ExtraDirectory,
    DirectoryCaseMismatch,
    MissingTxtFile,
    ExtraTxtFile,
    TxtFileCaseMismatch,
    NonTxtFile,
    NestedDirectory,
    EmptyTxtFile,
    OneLineTxtFile,
    UnreadableTxtFile,
}

