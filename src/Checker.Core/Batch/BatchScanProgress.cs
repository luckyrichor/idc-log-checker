using IDCLogChecker.Core.Scanning;

namespace IDCLogChecker.Core.Batch;

public sealed record BatchScanProgress(
    int CompletedFolders,
    int TotalFolders,
    int FolderIndex,
    string FolderPath,
    ScanProgress? DirectoryProgress);
