using System.Runtime.InteropServices;

namespace CareHR.UhfCardWriter.Sdk.Native;

/// <summary>
/// Compile/runtime layout checks for native structs (Phase 2).
/// Expected sizes/offsets match CFApi.h field order + SDK Sequential sample.
/// </summary>
internal static class NativeLayout
{
    /// <summary>Throws if SizeOf/OffsetOf diverge from CFApi.h expectations.</summary>
    public static void ValidateOrThrow()
    {
        AssertSize(typeof(NativeTagInfo), NativeStructs.TagInfoSize);
        AssertOffset(typeof(NativeTagInfo), nameof(NativeTagInfo.NO), 0);
        AssertOffset(typeof(NativeTagInfo), nameof(NativeTagInfo.Rssi), 2);
        AssertOffset(typeof(NativeTagInfo), nameof(NativeTagInfo.Antenna), 4);
        AssertOffset(typeof(NativeTagInfo), nameof(NativeTagInfo.Channel), 5);
        AssertOffset(typeof(NativeTagInfo), nameof(NativeTagInfo.Crc), 6);
        AssertOffset(typeof(NativeTagInfo), nameof(NativeTagInfo.Pc), 8);
        AssertOffset(typeof(NativeTagInfo), nameof(NativeTagInfo.CodeLength), 10);
        AssertOffset(typeof(NativeTagInfo), nameof(NativeTagInfo.Code), 11);

        AssertSize(typeof(NativeTagResp), NativeStructs.TagRespSize);
        AssertOffset(typeof(NativeTagResp), nameof(NativeTagResp.TagStatus), 0);
        AssertOffset(typeof(NativeTagResp), nameof(NativeTagResp.Antenna), 1);
        AssertOffset(typeof(NativeTagResp), nameof(NativeTagResp.Crc), 2);
        AssertOffset(typeof(NativeTagResp), nameof(NativeTagResp.Pc), 4);
        AssertOffset(typeof(NativeTagResp), nameof(NativeTagResp.CodeLength), 6);
        AssertOffset(typeof(NativeTagResp), nameof(NativeTagResp.Code), 7);

        AssertSize(typeof(NativeDevicePara), NativeStructs.DeviceParaSize);
        AssertOffset(typeof(NativeDevicePara), nameof(NativeDevicePara.DeviceAddr), 0);
        AssertOffset(typeof(NativeDevicePara), nameof(NativeDevicePara.Region), 7);
        AssertOffset(typeof(NativeDevicePara), nameof(NativeDevicePara.StartFreI), 8);
        AssertOffset(typeof(NativeDevicePara), nameof(NativeDevicePara.StartFreD), 10);
        AssertOffset(typeof(NativeDevicePara), nameof(NativeDevicePara.StepFre), 12);
        AssertOffset(typeof(NativeDevicePara), nameof(NativeDevicePara.Cn), 14);
        AssertOffset(typeof(NativeDevicePara), nameof(NativeDevicePara.InternalTime), 24);
    }

    private static void AssertSize(Type type, int expected)
    {
        var actual = Marshal.SizeOf(type);
        if (actual != expected)
            throw new InvalidOperationException($"Native layout size mismatch: {type.Name} SizeOf={actual}, expected={expected}.");
    }

    private static void AssertOffset(Type type, string field, int expected)
    {
        var actual = (int)Marshal.OffsetOf(type, field);
        if (actual != expected)
            throw new InvalidOperationException($"Native layout offset mismatch: {type.Name}.{field} OffsetOf={actual}, expected={expected}.");
    }
}
