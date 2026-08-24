namespace IDCLogChecker.Core.ContentAnalysis;

public enum DeviceFamily
{
    Unknown,
    S5552,
    CE16808,
    N18010,
    S12508,
    S12516,
    S7610,
    Model9908X,
    HW9303,
    HW9306,
}

public static class DeviceFamilyResolver
{
    public static DeviceFamily Resolve(string deviceName, NormalizedCommandOutput output)
    {
        var name = deviceName.ToUpperInvariant();
        if (name.Contains("S5552", StringComparison.Ordinal)) return DeviceFamily.S5552;
        if (name.Contains("CE16808", StringComparison.Ordinal)) return DeviceFamily.CE16808;
        if (name.Contains("N18010", StringComparison.Ordinal)) return DeviceFamily.N18010;
        if (name.Contains("S12508", StringComparison.Ordinal)) return DeviceFamily.S12508;
        if (name.Contains("S12516", StringComparison.Ordinal)) return DeviceFamily.S12516;
        if (name.Contains("S7610", StringComparison.Ordinal)) return DeviceFamily.S7610;
        if (name.Contains("9908X", StringComparison.Ordinal)) return DeviceFamily.Model9908X;
        if (name.Contains("HW9303", StringComparison.Ordinal)) return DeviceFamily.HW9303;
        if (name.Contains("HW9306", StringComparison.Ordinal)) return DeviceFamily.HW9306;
        return DeviceFamily.Unknown;
    }
}
