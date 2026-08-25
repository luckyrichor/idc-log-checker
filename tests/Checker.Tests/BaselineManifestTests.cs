using IDCLogChecker.Core.Baseline;
using Xunit;

namespace IDCLogChecker.Tests;

public sealed class BaselineManifestTests
{
    [Fact]
    public void EmbeddedManifestContainsTheVerifiedReferenceSet()
    {
        var manifest = BaselineManifest.LoadEmbedded();

        Assert.Equal(1, manifest.SchemaVersion);
        Assert.Equal(62, manifest.Devices.Count);
        Assert.Equal(3660, manifest.Devices.Sum(device => device.TxtFiles.Count));
        Assert.Contains(
            manifest.Devices,
            device => device.Name == "SHPD-NQSJZX-4F202-SW01-9908X"
                      && device.TxtFiles.Contains("show debug.txt", StringComparer.Ordinal));
        Assert.Contains(
            manifest.Devices,
            device => device.Name == "SHPT-NJSJZX-11B3FWL-SW02-CE16808"
                      && device.TxtFiles.Contains("display version.txt", StringComparer.Ordinal));
    }

    [Fact]
    public void EmbeddedManifestHasNoDuplicateDeviceOrFileNames()
    {
        var manifest = BaselineManifest.LoadEmbedded();

        Assert.Equal(
            manifest.Devices.Count,
            manifest.Devices.Select(device => device.Name).Distinct(StringComparer.Ordinal).Count());
        Assert.All(
            manifest.Devices,
            device => Assert.Equal(
                device.TxtFiles.Count,
                device.TxtFiles.Distinct(StringComparer.Ordinal).Count()));
    }
}
