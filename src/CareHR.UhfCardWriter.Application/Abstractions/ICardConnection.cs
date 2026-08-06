using CareHR.UhfCardWriter.Application.Devices;

namespace CareHR.UhfCardWriter.Application.Abstractions;

/// <summary>
/// Application port for CareHR card reader connection and USB discovery.
/// </summary>
public interface ICardConnection
{
    /// <summary>Gets a value indicating whether a reader session is open.</summary>
    bool IsOpen { get; }

    /// <summary>Opens a serial (COM) connection.</summary>
    DeviceResult OpenSerial(string comPort, int baudRate);

    /// <summary>Opens a USB HID connection by device index.</summary>
    DeviceResult OpenHid(ushort index);

    /// <summary>Opens a TCP network connection.</summary>
    DeviceResult OpenNet(string ip, ushort port, int timeoutMs);

    /// <summary>Closes the current session.</summary>
    DeviceResult Close();

    /// <summary>Gets the USB HID device count.</summary>
    DeviceResult<int> GetUsbDeviceCount();

    /// <summary>Gets USB device info text for an index.</summary>
    DeviceResult<string> GetUsbDeviceInfo(ushort index, int capacity = DeviceConstants.DefaultUsbInfoCapacity);
}
