using System.Text;
using IDCLogChecker.Core.Baseline;
using IDCLogChecker.Core.Scanning;

namespace IDCLogChecker.Tests;

internal sealed class FixtureCase : IDisposable
{
    public FixtureCase(string workPath, string scanPath, DirectoryScanner scanner)
    {
        WorkPath = workPath;
        ScanPath = scanPath;
        Scanner = scanner;
    }

    public string WorkPath { get; }
    public string ScanPath { get; }
    public DirectoryScanner Scanner { get; }

    public void Dispose()
    {
        if (Directory.Exists(WorkPath)) Directory.Delete(WorkPath, recursive: true);
    }
}

internal static class FixtureCaseFactory
{
    private static readonly IReadOnlyList<BaselineDevice> Devices =
    [
        new BaselineDevice("Device-A", ["one.txt", "two.txt"]),
        new BaselineDevice("Device-B", ["status.txt"]),
    ];

    public static FixtureCase Create(string caseId)
    {
        var workPath = Path.Combine(Path.GetTempPath(), "IDC日志检查 用例", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workPath);
        var scanPath = caseId == "16-chinese-space-path"
            ? Path.Combine(workPath, "中文 空格 日志目录")
            : Path.Combine(workPath, "logs");

        if (caseId == "02-root-missing")
        {
            return new FixtureCase(workPath, Path.Combine(workPath, "不存在"), new DirectoryScanner(Devices));
        }

        if (caseId == "03-wrong-root-level")
        {
            CreateValid(Path.Combine(workPath, "logs"));
            return new FixtureCase(workPath, workPath, new DirectoryScanner(Devices));
        }

        CreateValid(scanPath);
        switch (caseId)
        {
            case "04-missing-directory":
                Directory.Delete(Path.Combine(scanPath, "Device-B"), recursive: true);
                break;
            case "05-extra-directory":
                Directory.CreateDirectory(Path.Combine(scanPath, "Device-C"));
                break;
            case "06-directory-case":
                Directory.Delete(Path.Combine(scanPath, "Device-A"), recursive: true);
                CreateDevice(scanPath, "device-a", ["one.txt", "two.txt"]);
                break;
            case "07-missing-txt":
                File.Delete(Path.Combine(scanPath, "Device-A", "two.txt"));
                break;
            case "08-extra-txt":
                WriteValid(Path.Combine(scanPath, "Device-A", "extra.txt"));
                break;
            case "09-txt-case":
                File.Delete(Path.Combine(scanPath, "Device-A", "one.txt"));
                WriteValid(Path.Combine(scanPath, "Device-A", "ONE.txt"));
                break;
            case "10-zero-byte":
                File.WriteAllBytes(Path.Combine(scanPath, "Device-A", "one.txt"), []);
                break;
            case "11-bom-only":
                File.WriteAllBytes(Path.Combine(scanPath, "Device-A", "one.txt"), Encoding.UTF8.Preamble);
                break;
            case "12-whitespace-only":
                File.WriteAllText(Path.Combine(scanPath, "Device-A", "one.txt"), " \t\r\n  ");
                break;
            case "13-one-line":
                File.WriteAllText(Path.Combine(scanPath, "Device-A", "one.txt"), "只有这一行");
                break;
            case "14-non-txt":
                File.WriteAllText(Path.Combine(scanPath, "Device-A", "说明.csv"), "附加说明");
                break;
            case "15-nested-directory":
                Directory.CreateDirectory(Path.Combine(scanPath, "Device-A", "archive"));
                break;
            case "17-multiple-findings":
                Directory.Delete(Path.Combine(scanPath, "Device-B"), recursive: true);
                File.WriteAllBytes(Path.Combine(scanPath, "Device-A", "one.txt"), []);
                WriteValid(Path.Combine(scanPath, "Device-A", "extra.txt"));
                break;
        }

        return new FixtureCase(workPath, scanPath, new DirectoryScanner(Devices));
    }

    private static void CreateValid(string root)
    {
        CreateDevice(root, "Device-A", ["one.txt", "two.txt"]);
        CreateDevice(root, "Device-B", ["status.txt"]);
    }

    private static void CreateDevice(string root, string name, IEnumerable<string> files)
    {
        foreach (var file in files) WriteValid(Path.Combine(root, name, file));
    }

    private static void WriteValid(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "第一行\r\n第二行\r\n");
    }
}
