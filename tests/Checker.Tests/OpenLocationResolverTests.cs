using IDCLogChecker.Core.Presentation;
using Xunit;

namespace IDCLogChecker.Tests;

public sealed class OpenLocationResolverTests
{
    [Fact]
    public void MissingTxtUsesExistingDeviceDirectory()
    {
        using var fixture = new TestDirectory();
        var device = fixture.CreateDirectory("Device-A");

        var target = OpenLocationResolver.Resolve(Path.Combine(device, "missing.txt"));

        Assert.NotNull(target);
        Assert.Equal(device, target.Path);
        Assert.False(target.SelectFile);
    }

    [Fact]
    public void MissingDeviceUsesExistingScanRoot()
    {
        using var fixture = new TestDirectory();

        var target = OpenLocationResolver.Resolve(Path.Combine(fixture.Path, "Device-Missing", "missing.txt"));

        Assert.NotNull(target);
        Assert.Equal(fixture.Path, target.Path);
        Assert.False(target.SelectFile);
    }

    [Fact]
    public void ExistingFileIsSelected()
    {
        using var fixture = new TestDirectory();
        var file = fixture.WriteFile("present.txt", "ok");

        var target = OpenLocationResolver.Resolve(file);

        Assert.NotNull(target);
        Assert.Equal(file, target.Path);
        Assert.True(target.SelectFile);
    }

    [Fact]
    public void ExistingDirectoryIsOpenedWithoutSelection()
    {
        using var fixture = new TestDirectory();

        var target = OpenLocationResolver.Resolve(fixture.Path);

        Assert.NotNull(target);
        Assert.Equal(fixture.Path, target.Path);
        Assert.False(target.SelectFile);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("missing-relative-item.txt")]
    public void InvalidPathWithoutExistingAncestorReturnsNull(string? path) =>
        Assert.Null(OpenLocationResolver.Resolve(path));
}
