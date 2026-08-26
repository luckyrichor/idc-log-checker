using IDCLogChecker.Core.Presentation;
using Xunit;

namespace IDCLogChecker.Tests;

public sealed class WindowsExplorerLaunchTests
{
    [Fact]
    public void FileSelectionArgumentQuotesPathsContainingSpaces()
    {
        var target = new OpenLocationTarget(
            @"C:\测试用例\17-multiple-findings\设备\display cpu.txt",
            SelectFile: true);

        var launch = WindowsExplorerLaunch.Build(target);

        Assert.Equal("explorer.exe", launch.FileName);
        Assert.Equal(
            @"/select,""C:\测试用例\17-multiple-findings\设备\display cpu.txt""",
            launch.Arguments);
    }

    [Fact]
    public void DirectoryArgumentQuotesPathsContainingSpaces()
    {
        var target = new OpenLocationTarget(
            @"C:\测试用例\10-zero-byte\设备目录",
            SelectFile: false);

        var launch = WindowsExplorerLaunch.Build(target);

        Assert.Equal(@"""C:\测试用例\10-zero-byte\设备目录""", launch.Arguments);
    }
}
