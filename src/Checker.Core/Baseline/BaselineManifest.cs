using System.Reflection;
using System.Text.Json;

namespace IDCLogChecker.Core.Baseline;

public sealed record BaselineDevice(string Name, IReadOnlyList<string> TxtFiles);

public sealed class BaselineManifest
{
    private const string ResourceName = "IDCLogChecker.Core.Baseline.manifest.json";

    private BaselineManifest(
        int schemaVersion,
        IReadOnlyList<string> sourceSnapshots,
        string baselineSha256,
        IReadOnlyList<BaselineDevice> devices)
    {
        SchemaVersion = schemaVersion;
        SourceSnapshots = sourceSnapshots;
        BaselineSha256 = baselineSha256;
        Devices = devices;
    }

    public int SchemaVersion { get; }

    public IReadOnlyList<string> SourceSnapshots { get; }

    public string BaselineSha256 { get; }

    public IReadOnlyList<BaselineDevice> Devices { get; }

    public static BaselineManifest LoadEmbedded()
    {
        var assembly = typeof(BaselineManifest).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"找不到内置基准资源: {ResourceName}");

        var dto = JsonSerializer.Deserialize<ManifestDto>(stream, JsonOptions)
            ?? throw new InvalidDataException("内置基准内容为空。");

        if (dto.SchemaVersion != 1)
        {
            throw new InvalidDataException($"不支持的基准版本: {dto.SchemaVersion}");
        }

        var devices = dto.Devices
            .Select(device => new BaselineDevice(device.Name, device.TxtFiles.AsReadOnly()))
            .ToArray();

        if (devices.Select(device => device.Name).Distinct(StringComparer.Ordinal).Count() != devices.Length)
        {
            throw new InvalidDataException("内置基准包含重复设备目录名。");
        }

        foreach (var device in devices)
        {
            if (device.TxtFiles.Distinct(StringComparer.Ordinal).Count() != device.TxtFiles.Count)
            {
                throw new InvalidDataException($"内置基准包含重复文件名: {device.Name}");
            }
        }

        return new BaselineManifest(
            dto.SchemaVersion,
            dto.SourceSnapshots.AsReadOnly(),
            dto.BaselineSha256,
            devices);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private sealed class ManifestDto
    {
        public int SchemaVersion { get; init; }

        public List<string> SourceSnapshots { get; init; } = [];

        public string BaselineSha256 { get; init; } = string.Empty;

        public List<DeviceDto> Devices { get; init; } = [];
    }

    private sealed class DeviceDto
    {
        public string Name { get; init; } = string.Empty;

        public List<string> TxtFiles { get; init; } = [];
    }
}
