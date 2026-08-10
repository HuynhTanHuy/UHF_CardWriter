namespace CareHR.UhfCardWriter.Sdk.Models;

/// <summary>
/// Managed mirror of CFApi.h <c>DevicePara</c> / sample <c>Devicepara</c>.
/// Field <see cref="Interface"/> maps to vendor <c>INTERFACE</c> (Out Interface).
/// </summary>
public sealed class DeviceParameters
{
    public byte DeviceAddr { get; set; }
    public byte RfidPro { get; set; }
    public byte WorkMode { get; set; }

    /// <summary>Vendor <c>INTERFACE</c> / sample <c>port</c>.</summary>
    public byte Interface { get; set; }

    public byte BaudRate { get; set; }
    public byte WgSet { get; set; }
    public byte Ant { get; set; }
    public byte Region { get; set; }
    public ushort StartFreI { get; set; }
    public ushort StartFreD { get; set; }
    public ushort StepFre { get; set; }
    public byte Cn { get; set; }
    public byte RfidPower { get; set; }
    public byte InventoryArea { get; set; }
    public byte QValue { get; set; }
    public byte Session { get; set; }
    public byte AcsAddr { get; set; }
    public byte AcsDataLen { get; set; }
    public byte FilterTime { get; set; }
    public byte TriggleTime { get; set; }
    public byte BuzzerTime { get; set; }
    public byte InternalTime { get; set; }
}
