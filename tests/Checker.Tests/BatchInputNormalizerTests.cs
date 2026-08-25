using IDCLogChecker.Core.Batch;
using Xunit;

namespace IDCLogChecker.Tests;

public sealed class BatchInputNormalizerTests
{
    [Fact]
    public void KeepsValidFoldersInOrderAndRemovesDuplicates()
    {
        using var fixture = new TestDirectory();
        var first = fixture.CreateDirectory("第一批");
        var second = fixture.CreateDirectory("第二批");

        var result = BatchInputNormalizer.Normalize([first, second, first]);

        Assert.Equal([Path.GetFullPath(first), Path.GetFullPath(second)], result.ValidPaths);
        Assert.Single(result.DuplicatePaths);
        Assert.Equal(Path.GetFullPath(first), result.DuplicatePaths[0]);
    }

    [Fact]
    public void MixedInputKeepsFoldersAndExplainsFilesMissingAndBlankItems()
    {
        using var fixture = new TestDirectory();
        var folder = fixture.CreateDirectory("有效目录");
        var file = fixture.WriteFile("普通文件.txt");
        var missing = Path.Combine(fixture.Path, "不存在");

        var result = BatchInputNormalizer.Normalize([file, folder, missing, " ", null]);

        Assert.Equal([Path.GetFullPath(folder)], result.ValidPaths);
        Assert.Equal(4, result.SkippedItems.Count);
        Assert.Contains(result.SkippedItems, item => item.Input == file && item.Reason.Contains("文件，不是文件夹"));
        Assert.Contains(result.SkippedItems, item => item.Input == missing && item.Reason.Contains("不存在"));
        Assert.Contains(result.SkippedItems, item => string.IsNullOrWhiteSpace(item.Input));
    }

    [Fact]
    public void WhollyInvalidInputDoesNotClaimToHaveValidFolders()
    {
        var result = BatchInputNormalizer.Normalize(["/definitely/not/a/real/idc/folder"]);

        Assert.False(result.HasValidPaths);
        Assert.Empty(result.ValidPaths);
        Assert.Single(result.SkippedItems);
    }
}
